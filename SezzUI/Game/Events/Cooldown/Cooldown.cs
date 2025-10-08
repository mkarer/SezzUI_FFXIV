/*
* LibSezzCooldown-16
* Initial port with basic functionality...
*
* Just like in World of Warcraft we don't care about the accumulated duration of all charges,
* instead we only care about one charge.
*
* IMPORTANT: Only ActionType.Action is supported, to watch "General" actions lookup their action ID first.
* This can easily be done by enabling debug logging and using the action, SendAction and ReceiveActionEffect
* should output the correct ID.
*
* TODO: Remove all unused action type code.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using JetBrains.Annotations;
using SezzUI.Helper;
using SezzUI.Hooking;
using SezzUI.Modules;

namespace SezzUI.Game.Events.Cooldown;

internal sealed unsafe class Cooldown : BaseEvent, IHookAccessor
{
	List<IHookWrapper>? IHookAccessor.Hooks { get; set; }

#pragma warning disable 67
	public delegate void CooldownChangedDelegate(uint actionId, CooldownData data, bool chargesChanged, ushort previousCharges);

	public event CooldownChangedDelegate? CooldownChanged;

	public delegate void CooldownStartedDelegate(uint actionId, CooldownData data);

	public event CooldownStartedDelegate? CooldownStarted;

	public delegate void CooldownFinishedDelegate(uint actionId, CooldownData data, uint elapsedFinish);

	public event CooldownFinishedDelegate? CooldownFinished;
#pragma warning restore 67

	private delegate void SendActionDelegate(ulong targetObjectId, byte actionType, uint actionId, ushort sequence, long a5, long a6, long a7, long a8, long a9);

	private readonly HookWrapper<SendActionDelegate>? _sendActionHook;

	private delegate void ReceiveActionEffectDelegate(ulong sourceActorId, IntPtr sourceActor, IntPtr vectorPosition, IntPtr effectHeader, IntPtr effectArray, IntPtr effectTrail);

	private readonly HookWrapper<ReceiveActionEffectDelegate>? _receiveActionEffectHook;

	// TODO: Delay first update based on latency?
	// Also: https://twitter.com/perchbird_/status/1282734091780186120
	private static readonly uint UPDATE_DELAY_INITIAL = 1850;
	private static readonly uint UPDATE_DELAY_REPEATED = 5000;
	private static readonly uint UPDATE_DELAY_REPEATED_FREQUENT = 50; // 60 FPS = 16.7ms/Frame

	private readonly Dictionary<uint, ushort> _watchedActions = new();
	private readonly Dictionary<uint, CooldownData> _cache = new();

	private readonly List<Tuple<long, uint>> _delayedUpdates = new();
	private readonly DelayedCooldownUpdateComparer _delayedCooldownUpdateComparer = new();

	private static readonly Dictionary<uint, uint> _actionsModifyingCooldowns = new() // Actions that modify the duration of other cooldowns
	{
		// WAR
		// Enhanced Infuriate [157]: Reduces Infuriate [52] recast time by 5 seconds upon landing Inner Beast [49], Steel Cyclone [51], Fell Cleave [3549], or Decimate [3550] on most targets.
		{49, 52},
		{51, 52},
		{3549, 52},
		{3550, 52},
		// BRD
		// Empyreal Arrow [3885] -> Bloodletter [110] (and Rain of Death [117] which is grouped below) during Mage's Ballad
		{3558, 110}
		// SMN
		// XIVCombo Demi Enkindle Feature: Enkindle [7429] somehow is still 7427?
		//{ 7427, 7429 },
	};

	private static readonly List<List<uint>> _cooldownGroups = new() // Actions that share the same cooldown
	{
		// SAM
		new() {7867, 16483, 16486, 16485, 16484}, // (Iaijutsu (XIVCombo) [7867]) Tsubame-gaeshi [16483] => Kaeshi: Setsugekka [16486] + Kaeshi: Goken [16485] + Kaeshi: Higanbana [16484]
		// BRD
		new() {110, 117}, // Bloodletter [110] + Rain of Death [117]
		// RDM
		new() {16527, 7515}, // Engagement [16527] + Displacement [7515]
		// SMN
		new() {7427, 25831}, // Summon Bahamut [7427] + Summon Phoenix [25831]
		new() {7429, 16516}, // Enkindle Bahamut [7429] + Enkindle Phoenix [16516]
		// AST
		new() {37017, 37018}, // Astral Draw [37017] + Umbral Draw [37018]
		// WAR
		new() {3551, 16464}, // Raw Intuition [3551] + Nascent Flash [16464]
		// DRK
		new() {3625, 7390}, // Blood Weapon [3625], Delirium [7390]
		new() {3643, 3641} // Carve and Spirit [3643], Abyssal Drain [3641]
	};

	private static readonly List<uint> _frequentUpdateCooldowns = new() // Workaround until I figure out how to do this correctly. Yes, this is absolute bullshit :(
	{
		// BRD: Bloodletter [110], Rain of Death [117] => Mage's Ballad
		110,
		117
	};

	#region Public Methods

	public void Watch(uint actionId)
	{
		if (!(this as IPluginComponent).IsEnabled)
		{
			return;
		}

#if DEBUG
		if (Plugin.DebugConfig.LogEvents && Plugin.DebugConfig.LogEventCooldownUsage)
		{
			Logger.Debug($"Action ID: {actionId}");
		}
#endif

		if (_watchedActions.ContainsKey(actionId))
		{
			_watchedActions[actionId]++;
		}
		else
		{
			_watchedActions[actionId] = 1;
			Update(actionId, ActionType.Action);
		}
	}

	public void Unwatch(uint actionId, bool all = false)
	{
		if (!(this as IPluginComponent).IsEnabled || !_watchedActions.ContainsKey(actionId))
		{
			return;
		}

#if DEBUG
		if (Plugin.DebugConfig.LogEvents && Plugin.DebugConfig.LogEventCooldownUsage)
		{
			Logger.Debug($"Action ID: {actionId} All: {all}");
		}
#endif

		if (!all)
		{
			_watchedActions[actionId]--;
		}

		if (all || _watchedActions[actionId] == 0)
		{
			_watchedActions.Remove(actionId);
			_delayedUpdates.RemoveAll(x => x.Item2 == actionId);
			_cache.Remove(actionId);
		}
	}

	public CooldownData Get(uint actionId)
	{
		if ((this as IPluginComponent).IsEnabled && !_watchedActions.ContainsKey(actionId))
		{
			Logger.Error($"Error: Cannot retrieve data for unwatched cooldown! Action ID: {actionId}");
		}
		else if (_cache.ContainsKey(actionId))
		{
			return _cache[actionId];
		}

		return new();
	}

	#endregion

	#region Cooldown Data Update

	private bool Update(uint actionId, ActionType actionType)
	{
		IPlayerCharacter? player = Services.ClientState.LocalPlayer;
		if (player == null)
		{
			return false;
		}

		// We're not adjusting the action ID in here, because actionId should already be adjusted.
		GetRecastTimes(actionId, out float totalCooldown, out float totalElapsed, actionType);
		ushort maxChargesMaxLevel = GetMaxCharges(actionId, Constants.MAX_PLAYER_LEVEL);
		ushort maxChargesCurrentLevel = player.Level < Constants.MAX_PLAYER_LEVEL ? GetMaxCharges(actionId, player.Level) : maxChargesMaxLevel;
		float chargesMod = maxChargesCurrentLevel != maxChargesMaxLevel ? 1f / maxChargesMaxLevel * maxChargesCurrentLevel : 1;
		float totalCooldownAdjusted = totalCooldown * chargesMod; // GetRecastTime returns total cooldown for max level charges (but only if we have multiple charges right now?)

		//Logger.Debug($"maxChargesCurrentLevel {maxChargesCurrentLevel} maxChargesMaxLevel {maxChargesMaxLevel}");
		//Logger.Debug($"totalCooldown {totalCooldown} totalElapsed {totalElapsed} totalElapsedMS {(int)(totalElapsed * 1000f)} DateTime.UtcNow.Ticks {DateTime.UtcNow.Ticks} DateTime.UtcNow.Ticks-totalElapsed {DateTime.UtcNow.Subtract(TimeSpan.FromSeconds(totalElapsed)).Ticks}");
		//Logger.Debug($"totalCooldownAdjusted {totalCooldownAdjusted} chargesMod {chargesMod}");

		bool isFinished = totalCooldownAdjusted <= 0;
		if (!isFinished)
		{
			bool cooldownStarted = !_cache.ContainsKey(actionId);
			CooldownData data = cooldownStarted ? new() : _cache[actionId];
			data.PrepareUpdate();

			float cooldownPerCharge = totalCooldownAdjusted / maxChargesCurrentLevel;
			float totalElapsedAdjusted = Math.Min(totalCooldownAdjusted, totalElapsed); // GetRecastTimeElapsed is weird when maxChargesCurrentLevel differs from maxChargesMaxLevel
			float chargingMilliseconds = totalElapsedAdjusted % cooldownPerCharge * 1000;

			data.MaxCharges = maxChargesCurrentLevel;
			data.Duration = (uint) cooldownPerCharge * 1000;

			ushort previousCharges = data.CurrentCharges;

			data.CurrentCharges = totalElapsedAdjusted > 0 || maxChargesMaxLevel == 1 ? (ushort) Math.Floor(totalElapsedAdjusted / cooldownPerCharge) : maxChargesCurrentLevel;

			bool chargesChanged = !cooldownStarted && data.CurrentCharges != previousCharges;

			if (chargingMilliseconds > 0)
			{
				data.StartTime = Environment.TickCount64 - (int) chargingMilliseconds; // Not 100% accurate for some reason, should be fine though (if not we can still update more often).
			}

			data.Type = actionType;

			//Logger.Debug($"now {Environment.TickCount64}");
			//Logger.Debug($"totalCooldownAdjusted {totalCooldownAdjusted}");
			//Logger.Debug($"totalElapsedAdjusted {totalElapsedAdjusted}");
			//Logger.Debug($"oneChargeCooldown {totalElapsedAdjusted % cooldownPerCharge}");
			//Logger.Debug($"totalCooldown {totalCooldown}");
			//Logger.Debug($"chargesMod {chargesMod}");
			//Logger.Debug($"StartTime {data.StartTime} ElapsedCalculated {Environment.TickCount64 - data.StartTime}ms elapsed {totalElapsedAdjusted % cooldownPerCharge}s {(totalElapsedAdjusted % cooldownPerCharge) * 1000}ms {(long)((totalElapsedAdjusted % cooldownPerCharge) * 1000)}ms ");

			isFinished = !data.IsActive;
			if (!isFinished)
			{
				// Schedule update
				uint delay = _frequentUpdateCooldowns.Contains(actionId) ? UPDATE_DELAY_REPEATED_FREQUENT : cooldownStarted ? UPDATE_DELAY_INITIAL : UPDATE_DELAY_REPEATED;
				uint remaining = data.Remaining;
				ScheduleUpdate(actionId, remaining >= delay ? delay : remaining + 1);

				if (cooldownStarted)
				{
					// New
					InvokeCooldownStarted(actionId, data);
					_cache[actionId] = data;
				}
				else
				{
					// Updated
					if (data.HasChanged)
					{
						InvokeCooldownChanged(actionId, data, chargesChanged, previousCharges);
					}
				}

				return true;
			}
		}

		if (isFinished)
		{
			// Inactive
			if (_cache.ContainsKey(actionId))
			{
				CooldownData data = _cache[actionId];
				data.CurrentCharges = data.MaxCharges;
				//Logger.Debug($"ActionID {actionId} FinishedBecauseTotalCooldownAdjusted {totalCooldownAdjusted <= 0} Now {Environment.TickCount64} StartTime {data.StartTime} Duration {data.Duration} Elapsed {Environment.TickCount64 - data.StartTime - data.Duration}");
				long elapsedFinished = Environment.TickCount64 - data.StartTime - data.Duration;
				InvokeCooldownFinished(actionId, data, elapsedFinished < 0 ? 0 : (uint) elapsedFinished); // elapsedFinished can be negative!
				_cache.Remove(actionId);
			}
		}

		return false;
	}

	private void UpdateAll()
	{
		foreach (KeyValuePair<uint, ushort> kvp in _watchedActions)
		{
			Update(kvp.Key, ActionType.Action);
		}
	}

	private bool TryUpdateIfWatched(uint actionId, ActionType actionType, [UsedImplicitly] bool isAdjusted = false, bool isGroupItem = false, bool isModifyingAction = false)
	{
		bool success = false;

		if (_watchedActions.ContainsKey(actionId))
		{
			// Action should be on cooldown now!
			if (!Update(actionId, actionType))
			{
				// Apparently it's not.
				ScheduleMultipleUpdates(actionId);
				//Logger.Debug($"ScheduleMultipleUpdates/1: {actionId} {SpellHelper.GetActionName(actionId) ?? "?"}");
			}

			success = true;
		}

		if (!isGroupItem && !isModifyingAction && _actionsModifyingCooldowns.ContainsKey(actionId))
		{
			// Delay update, cooldown won't get changed instantly.
			TryUpdateIfWatched(_actionsModifyingCooldowns[actionId], actionType, isAdjusted, isGroupItem, true);
			ScheduleMultipleUpdates(_actionsModifyingCooldowns[actionId]);
			//Logger.Debug($"ScheduleMultipleUpdates/2: {actionId} {SpellHelper.GetActionName(actionId) ?? "?"}");
			success = true;
		}

		if (!isGroupItem)
		{
			// Check if action is in a group and update watched group cooldowns
			foreach (List<uint> group in _cooldownGroups.Where(cdg => cdg.Contains(actionId)))
			{
				group.ForEach(groupedActionId => { TryUpdateAdjusted(groupedActionId, actionType, true); });
				success |= true;
			}
		}

		return success;
	}

	private bool TryUpdateAdjusted(uint actionId, ActionType actionType, bool isGroupItem = false)
	{
		if (!TryUpdateIfWatched(actionId, actionType, false, isGroupItem))
		{
			uint actionIdAdjusted = SpellHelper.GetAdjustedActionId(actionId);
			if (actionIdAdjusted != actionId)
			{
				return TryUpdateIfWatched(actionIdAdjusted, actionType, true, isGroupItem);
			}
		}
		else
		{
			return true;
		}

		return false;
	}

	#endregion

	#region Update Scheduling

	/// <summary>
	///     Schedule cooldown data update (in OnFrameworkUpdate)
	/// </summary>
	/// <param name="actionId"></param>
	/// <param name="delay">Minimum delay in milliseconds</param>
	private void ScheduleUpdate(uint actionId, uint delay)
	{
		lock (_delayedUpdates)
		{
			_delayedUpdates.Add(new(Environment.TickCount64 + delay, actionId));
			_delayedUpdates.Sort(_delayedCooldownUpdateComparer);
			//_delayedUpdates.ForEach(x => Logger.Debug($"_delayedUpdates {x.Item1} {x.Item2}"));
		}
	}

	private void ScheduleMultipleUpdates(uint actionIds)
	{
		// TODO: Latency-based/Action-queue time?
		ScheduleUpdate(actionIds, 25);
		ScheduleUpdate(actionIds, 50);
		ScheduleUpdate(actionIds, 75);
		ScheduleUpdate(actionIds, 150);
		ScheduleUpdate(actionIds, 300);
		ScheduleUpdate(actionIds, 500);
		ScheduleUpdate(actionIds, 600);
		ScheduleUpdate(actionIds, 900);
		ScheduleUpdate(actionIds, 2500);
	}

	private void OnFrameworkUpdate(IFramework framework)
	{
		if (_delayedUpdates.Any())
		{
			while (_delayedUpdates.Any() && _delayedUpdates[^1].Item1 <= Environment.TickCount64)
			{
				uint actionId = _delayedUpdates[^1].Item2;
				//Logger.Debug($"TickCount64 {Environment.TickCount64} UpdateTickCount64 {_delayedUpdates[^1].Item1} ActionID {actionId}");
				_delayedUpdates.RemoveAt(_delayedUpdates.Count - 1);
				Update(actionId, ActionType.Action);
			}
		}
	}

	#endregion

	#region ActionManager

	private readonly ActionManager* _actionManager;

	private void GetRecastTimes(uint actionId, out float total, out float elapsed, ActionType actionType = ActionType.Action)
	{
		total = 0f;
		elapsed = 0f;

		int recastGroup = _actionManager->GetRecastGroup((int) actionType, actionId);
		RecastDetail* recastDetail = _actionManager->GetRecastGroupDetail(recastGroup);
		if (recastDetail != null)
		{
			total = recastDetail->Total;
			elapsed = total > 0 ? recastDetail->Elapsed : 0;
		}
	}

	public static ushort GetMaxCharges(uint actionId, uint level = 0) => ActionManager.GetMaxCharges(actionId, level);

	#endregion

	#region Cooldown Events

	private void InvokeCooldownStarted(uint actionId, CooldownData data)
	{
#if DEBUG
		if (Plugin.DebugConfig.LogEvents && Plugin.DebugConfig.LogEventCooldownStarted)
		{
			Logger.Debug($"ActionID {actionId} StartTime {data.StartTime} Duration {data.Duration / 1000}s Charges {data.CurrentCharges}/{data.MaxCharges} Elapsed {Environment.TickCount64 - data.StartTime}ms Remaining {data.Remaining}ms");
		}
#endif
		try
		{
			CooldownStarted?.Invoke(actionId, data);
		}
		catch (Exception ex)
		{
			Logger.Error($"Failed invoking {nameof(CooldownStarted)}: {ex}");
		}
	}

	private void InvokeCooldownChanged(uint actionId, CooldownData data, bool chargesChanged, ushort previousCharges)
	{
#if DEBUG
		if (Plugin.DebugConfig.LogEvents && Plugin.DebugConfig.LogEventCooldownChanged)
		{
			Logger.Debug($"ActionID {actionId} StartTime {data.StartTime} Duration {data.Duration / 1000}s Charges {data.CurrentCharges}/{data.MaxCharges} (Charges Changed: {chargesChanged}{(chargesChanged ? $" Previously: {previousCharges}" : "")}) Elapsed {Environment.TickCount64 - data.StartTime}ms Remaining {data.Remaining}ms");
		}
#endif
		try
		{
			CooldownChanged?.Invoke(actionId, data, chargesChanged, previousCharges);
		}
		catch (Exception ex)
		{
			Logger.Error($"Failed invoking {nameof(CooldownChanged)}: {ex}");
		}
	}

	private void InvokeCooldownFinished(uint actionId, CooldownData data, uint elapsedFinish)
	{
#if DEBUG
		if (Plugin.DebugConfig.LogEvents && Plugin.DebugConfig.LogEventCooldownFinished)
		{
			Logger.Debug($"ActionID {actionId} StartTime {data.StartTime} Duration {data.Duration / 1000}s Charges {data.CurrentCharges}/{data.MaxCharges} Elapsed {Environment.TickCount64 - data.StartTime}ms Remaining {data.Remaining}ms ElapsedFinished {elapsedFinish}ms");
		}
#endif
		try
		{
			CooldownFinished?.Invoke(actionId, data, elapsedFinish);
		}
		catch (Exception ex)
		{
			Logger.Error($"Failed invoking {nameof(CooldownFinished)}: {ex}");
		}
	}

	#endregion

	#region Hook

	private void SendActionDetour(ulong targetObjectId, byte actionType, uint actionId, ushort sequence, long a5, long a6, long a7, long a8, long a9)
	{
		_sendActionHook!.Original(targetObjectId, actionType, actionId, sequence, a5, a6, a7, a8, a9);
#if DEBUG
		if (Plugin.DebugConfig.LogEvents && Plugin.DebugConfig.LogEventCooldownHooks)
		{
			Logger.Debug($"Action ID: {actionId} Type: {actionType} Name: {((ActionType) actionType == ActionType.Action ? SpellHelper.GetActionName(actionId) ?? "?" : "?")} Target: 0x{targetObjectId:X} ({Services.Objects.SearchById((uint) targetObjectId)?.Name.TextValue ?? "??"})");
		}
#endif

		if ((ActionType) actionType != ActionType.Action || targetObjectId != Services.ClientState.LocalPlayer?.GameObjectId)
		{
			return;
		}

		TryUpdateIfWatched(actionId, (ActionType) actionType);
	}

	private void ReceiveActionEffectDetour(ulong sourceActorId, IntPtr sourceActor, IntPtr vectorPosition, IntPtr effectHeader, IntPtr effectArray, IntPtr effectTrail)
	{
		_receiveActionEffectHook!.Original(sourceActorId, sourceActor, vectorPosition, effectHeader, effectArray, effectTrail);

		if (sourceActorId != Services.ClientState.LocalPlayer?.GameObjectId || effectHeader == IntPtr.Zero)
		{
			return;
		}

		try
		{
			ActionEffectHeader header = Marshal.PtrToStructure<ActionEffectHeader>(effectHeader);
#if DEBUG
			if (Plugin.DebugConfig.LogEvents && Plugin.DebugConfig.LogEventCooldownHooks)
			{
				Logger.Debug($"Action ID: {header.ActionId} Type: {header.Type} Name: {((ActionType) header.Type == ActionType.Action ? SpellHelper.GetActionName(header.ActionId) ?? "?" : "?")} Source: 0x{sourceActorId:X} ({Services.Objects.SearchById((uint) sourceActorId)?.Name.TextValue ?? "??"}) Target: 0x{header.TargetObjectId:X} ({Services.Objects.SearchById((uint) header.TargetObjectId)?.Name.TextValue ?? "??"})");
			}
#endif
			if ((ActionType) header.Type == ActionType.Action)
			{
				TryUpdateIfWatched(header.ActionId, (ActionType) header.Type);
			}
		}
		catch (Exception ex)
		{
			Logger.Error(ex);
		}
	}

	#endregion

	#region Event Toggle

	protected override void OnEnable()
	{
		Services.Framework.Update += OnFrameworkUpdate;
		_receiveActionEffectHook?.Enable();
	}

	protected override void OnDisable()
	{
		Services.Framework.Update -= OnFrameworkUpdate;
		_receiveActionEffectHook?.Disable();
		_delayedUpdates.Clear();
		_cache.Clear();
	}

	#endregion

	public Cooldown()
	{
		_actionManager = ActionManager.Instance();
		_sendActionHook = (this as IHookAccessor).Hook<SendActionDelegate>("48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 57 48 81 EC ?? ?? ?? ?? 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 84 24 ?? ?? ?? ?? 48 8B E9 41 0F B7 D9", SendActionDetour); // https://github.com/44451516/XIVSlothCombo/blob/CN/XIVSlothComboX/Core/HookAddress.cs#L23
		_receiveActionEffectHook = (this as IHookAccessor).Hook<ReceiveActionEffectDelegate>(ActionEffectHandler.Addresses.Receive.Value, ReceiveActionEffectDetour);

		(this as IPluginComponent).Enable();
	}

	protected override void OnDispose()
	{
		_watchedActions.Clear();
		_actionsModifyingCooldowns.Clear();
		_cooldownGroups.Clear();
		_frequentUpdateCooldowns.Clear();
		_receiveActionEffectHook?.Dispose();
		_sendActionHook?.Dispose();
	}
}

internal class DelayedCooldownUpdateComparer : IComparer<Tuple<long, uint>>
{
	public int Compare(Tuple<long, uint>? x, Tuple<long, uint>? y)
	{
		if (x == null && y == null)
		{
			return 0;
		}

		if (x == null)
		{
			return 1;
		}

		if (y == null)
		{
			return -1;
		}

		return y.Item1.CompareTo(x.Item1);
	}
}