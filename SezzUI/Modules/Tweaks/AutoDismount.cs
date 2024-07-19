// https://github.com/akira0245/SmartCast/blob/master/SmartCast.cs
// https://github.com/FFXIV-CombatReborn/BossmodReborn/blob/main/BossMod/Framework/ActionManagerEx.cs

using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using Lumina.Excel.GeneratedSheets;
using SezzUI.Configuration;
using SezzUI.Helper;
using SezzUI.Hooking;
using Action = Lumina.Excel.GeneratedSheets.Action;

namespace SezzUI.Modules.Tweaks;

public class AutoDismount : PluginModule, IHookAccessor
{
	private AutoDismountConfig Config => (AutoDismountConfig) _config;
#if DEBUG
	private readonly AutoDismountDebugConfig _debugConfig;
#endif

	List<IHookWrapper>? IHookAccessor.Hooks { get; set; }
	private static HookWrapper<UseActionDelegate>? _useActionHook;

	// ReSharper disable once InconsistentNaming
	public unsafe delegate bool UseActionDelegate(ActionManager* self, ActionType actionType, uint actionId, ulong targetId, uint itemLocation, uint callType, uint comboRouteID, bool* outOptGTModeStarted);

	private Dictionary<uint, Action> _dismountActions;
	private Dictionary<uint, Action> _groundTargetActions;
	private HashSet<uint> _battleJobs;

	private QueuedAction? _queuedAction;
	private DateTime? _queuedActionTime;

	protected override void OnEnable()
	{
		_dismountActions = Services.Data.GetExcelSheet<Action>()!.Where(i => i.IsPlayerAction && i.RowId > 8 && i.ActionCategory?.Value?.RowId is 2 or 3 or 4 or 9 or 15).ToDictionary(i => i.RowId, j => j);
		_groundTargetActions = Services.Data.GetExcelSheet<Action>()!.Where(i => i.TargetArea && i.RowId != 7419 && i.RowId != 3573).ToDictionary(i => i.RowId, j => j);
		_battleJobs = Services.Data.GetExcelSheet<ClassJob>()!.Where(i => i.ClassJobCategory?.Value?.RowId is 30 or 31).Select(i => i.RowId).ToHashSet();
	}

	// ReSharper disable once InconsistentNaming
	private unsafe bool UseActionDetour(ActionManager* self, ActionType actionType, uint actionId, ulong targetId, uint itemLocation, uint callType, uint comboRouteID, bool* outOptGTModeStarted)
	{
#if DEBUG
		if (_debugConfig.LogAllUsedActions)
		{
			Logger.Debug($"[UseAction] Type: {actionType} ID: {actionId} AdjustedID: {SpellHelper.GetAdjustedActionId(actionId)} (DismountAction: {_dismountActions.ContainsKey(SpellHelper.GetAdjustedActionId(actionId))}) TargetID: {targetId} ItemLocation: {itemLocation} CallType: {callType} ComboRouteID: {comboRouteID}");
		}
#endif
		if (Services.Condition[ConditionFlag.Mounted] && (actionType == ActionType.Action || (actionType == ActionType.GeneralAction && actionId == 4))) // 4: Sprint
		{
			uint actionIdAdjusted = SpellHelper.GetAdjustedActionId(actionId);

			if (Services.ClientState.LocalPlayer != null && _battleJobs.Contains(Services.ClientState.LocalPlayer.ClassJob.Id) && (_dismountActions.ContainsKey(actionIdAdjusted) || (actionType == ActionType.GeneralAction && actionId == 4)))
			{
				if (Config.AutoCast)
				{
#if DEBUG
					if (_debugConfig.LogQueuedActions)
					{
						Logger.Debug($"Queueing: ID: {actionId} Adjusted ID: {actionIdAdjusted} Name: {(actionType == ActionType.GeneralAction ? SpellHelper.GetGeneralActionName(actionIdAdjusted) : SpellHelper.GetActionName(actionIdAdjusted))}");
					}
#endif
					_queuedAction = new(actionType, actionIdAdjusted, targetId, itemLocation, callType, comboRouteID);
					_queuedActionTime = DateTime.Now.AddSeconds(1);
					Services.Framework.Update += ExecuteQueuedAction;
				}
#if DEBUG
				if (_debugConfig.LogGeneral)
				{
					Logger.Debug("Dismounting.");
				}
#endif
				return _useActionHook!.Original(self, ActionType.GeneralAction, 23, 0xE0000000, 0, 0, 0, outOptGTModeStarted);
			}
		}

		return _useActionHook!.Original(self, actionType, actionId, targetId, itemLocation, callType, comboRouteID, outOptGTModeStarted);
	}

	private unsafe void ExecuteQueuedAction(IFramework framework)
	{
		if (_queuedAction == null || _queuedActionTime == null)
		{
			Services.Framework.Update -= ExecuteQueuedAction;
		}
		else if (DateTime.Now >= _queuedActionTime)
		{
#if DEBUG
			if (_debugConfig.LogQueuedActions)
			{
				Logger.Debug($"Executing: ID: {_queuedAction.ActionId} Name: {(_queuedAction.ActionType == ActionType.GeneralAction ? SpellHelper.GetGeneralActionName(_queuedAction.ActionId) : SpellHelper.GetActionName(_queuedAction.ActionId))}");
			}
#endif
			bool areaTargeted = false;
			_useActionHook!.Original(ActionManager.Instance(), _queuedAction.ActionType, _queuedAction.ActionId, _queuedAction.TargetId, _queuedAction.ItemLocation, _queuedAction.CallType, _queuedAction.ComboRouteID, &areaTargeted);
			Services.Framework.Update -= ExecuteQueuedAction;
		}
	}

	protected override void OnDisable()
	{
		Services.Framework.Update -= ExecuteQueuedAction;
		_queuedAction = null;
		_queuedActionTime = null;
		_dismountActions.Clear();
		_groundTargetActions.Clear();
		_battleJobs.Clear();
	}

	public unsafe AutoDismount(PluginConfigObject config) : base(config)
	{
#if DEBUG
		_debugConfig = Singletons.Get<ConfigurationManager>().GetConfigObject<AutoDismountDebugConfig>();
#endif
		try
		{
			_useActionHook = (this as IHookAccessor).Hook<UseActionDelegate>(ActionManager.Addresses.UseAction.Value, UseActionDetour);
		}
		catch (Exception ex)
		{
			Logger.Error($"Failed to setup required hooks, disabling tweak. Error: {ex}");
			Services.Chat.PrintError("[SezzUI] AutoDismount disabled due to hooking issues.");
		}

		_dismountActions = new();
		_groundTargetActions = new();
		_battleJobs = new();

		if (_useActionHook != null)
		{
			Config.ValueChangeEvent += OnConfigPropertyChanged;
			Singletons.Get<ConfigurationManager>().Reset += OnConfigReset;
			(this as IPluginComponent).SetEnabledState(Config.Enabled);
		}
		else
		{
			(this as IPluginComponent).SetEnabledState(false);
		}
	}

	protected override void OnDispose()
	{
		Config.ValueChangeEvent -= OnConfigPropertyChanged;
		Singletons.Get<ConfigurationManager>().Reset -= OnConfigReset;
	}

	~AutoDismount()
	{
		Dispose(false);
	}

	#region Configuration Events

	private void OnConfigPropertyChanged(object sender, OnChangeBaseArgs args)
	{
		switch (args.PropertyName)
		{
			case "Enabled":
#if DEBUG
				if (_debugConfig.LogConfigurationManager)
				{
					Logger.Debug($"{args.PropertyName}: {Config.Enabled}");
				}
#endif
				(this as IPluginComponent).SetEnabledState(_useActionHook != null ? Config.Enabled : false);
				break;
		}
	}

	private void OnConfigReset(ConfigurationManager sender, PluginConfigObject config)
	{
		if (config != _config)
		{
			return;
		}

#if DEBUG
		if (_debugConfig.LogConfigurationManager)
		{
			Logger.Debug("Resetting...");
		}
#endif
		(this as IPluginComponent).Disable();
#if DEBUG
		if (_debugConfig.LogConfigurationManager)
		{
			Logger.Debug($"Config.Enabled: {Config.Enabled}");
		}
#endif
		(this as IPluginComponent).SetEnabledState(Config.Enabled);
	}

	#endregion

	private class QueuedAction
	{
		public readonly ActionType ActionType;
		public readonly uint ActionId;
		public readonly ulong TargetId;
		public readonly uint ItemLocation;
		public readonly uint CallType;
		public readonly uint ComboRouteID;

		// ReSharper disable once InconsistentNaming
		public QueuedAction(ActionType actionType, uint actionId, ulong targetId, uint itemLocation, uint callType, uint comboRouteID)
		{
			ActionType = actionType;
			ActionId = actionId;
			TargetId = targetId;
			ItemLocation = itemLocation;
			CallType = callType;
			ComboRouteID = comboRouteID;
		}
	}
}