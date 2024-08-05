using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.ClientState.Statuses;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Utility;
using Lumina.Excel;
using SezzUI.Enums;
using SezzUI.Hooking;
using SezzUI.Logging;
using LuminaAction = Lumina.Excel.GeneratedSheets.Action;
using LuminaActionIndirection = Lumina.Excel.GeneratedSheets.ActionIndirection;
using LuminaReplaceAction = Lumina.Excel.GeneratedSheets.ReplaceAction;
using LuminaGeneralAction = Lumina.Excel.GeneratedSheets.GeneralAction;
using LuminaStatus = Lumina.Excel.GeneratedSheets.Status;

namespace SezzUI.Helper;

public static class SpellHelper
{
	private static readonly ExcelSheet<LuminaAction>? _sheetAction;
	private static readonly ExcelSheet<LuminaGeneralAction>? _sheetGeneralAction;
	private static readonly ExcelSheet<LuminaStatus>? _sheetStatus;
	private static readonly Dictionary<uint, Dictionary<uint, uint>> _actionAdjustments;
	internal static PluginLogger Logger;

	static SpellHelper()
	{
		Logger = new("SpellHelper");

		_sheetAction = Services.Data.Excel.GetSheet<LuminaAction>();
		_sheetGeneralAction = Services.Data.Excel.GetSheet<LuminaGeneralAction>();
		_sheetStatus = Services.Data.Excel.GetSheet<LuminaStatus>();

		_actionAdjustments = new()
		{
			// Hardcoded actions that that will be used instead of calling GetAdjustedActionId.
			// The idea is that SpellHelper.GetAdjustedActionId should always only return the level appropriate action and don't care about combos.  
			// Combo plugin issues should be resolved now by OriginalFunctionManager.GetAdjustedActionId

			// ActionIndirection data is used to handle real combos ("Action changes to X while under the effect of Y." -> "This action cannot be assigned to a hotbar."):
			// https://github.com/xivapi/ffxiv-datamining/blob/master/csv/ActionIndirection.csv
			// ReplaceAction data is used for Dawntrail's "action change" feature:
			// https://github.com/xivapi/ffxiv-datamining/blob/master/csv/ReplaceAction.csv

			// Structure: [actionId] => { level, actionIdAtLevel }, { level, actionIdAtLevel }, ...

			// WAR
			{38, new() {{6, 38}, {70, 7389}}}, // Berserk
			{3551, new() {{56, 3551}, {82, 25751}}} // Raw Intuition
		};

		// TODO: Test at level 80, Aethercharge gets upgraded by traits and also is in ActionIndirection.
		// ActionIndirection data: 25800 (Aethercharge) -> 25831 (Summon Phoenix)
		List<uint> adjustmentWhitelist = new() {25800u};

		ExcelSheet<LuminaActionIndirection>? sheetActionIndirection = Services.Data.Excel.GetSheet<LuminaActionIndirection>();
		sheetActionIndirection?.Where(a => a.ClassJob.Value?.RowId > 0 && a.PreviousComboAction.Value is {RowId: > 0} && !adjustmentWhitelist.Contains(a.PreviousComboAction.Value.RowId)).ToList().ForEach(a => AddAdjustedAction(a.PreviousComboAction.Value!));

		ExcelSheet<LuminaReplaceAction>? sheetReplaceAction = Services.Data.Excel.GetSheet<LuminaReplaceAction>();
		sheetReplaceAction?.Where(a => a.Action.Value?.RowId > 0 && a.ReplaceAction1.Value is {RowId: > 0} && !adjustmentWhitelist.Contains(a.Action.Value.RowId)).ToList().ForEach(a => AddAdjustedAction(a.Action.Value!));
	}

	private static void AddAdjustedAction(LuminaAction action)
	{
		if (!_actionAdjustments.ContainsKey(action.RowId))
		{
			_actionAdjustments[action.RowId] = new() {{action.ClassJobLevel, action.RowId}};
		}
		else if (!_actionAdjustments[action.RowId].ContainsKey(action.ClassJobLevel))
		{
			// This should only be possible when there are already some hardcoded actions defined, let's keep it for now.
			_actionAdjustments[action.RowId][action.ClassJobLevel] = action.RowId;
		}
	}

	public static uint GetAdjustedActionId(uint actionId, bool allowCombo = false, bool debug = false)
	{
		byte level = Services.ClientState.LocalPlayer?.Level ?? 0;
		uint actionIdAdjusted = allowCombo ? OriginalFunctionManager.GetAdjustedActionId(actionId) : _actionAdjustments.TryGetValue(actionId, out Dictionary<uint, uint>? actionAdjustments) ? actionAdjustments.Where(a => level >= a.Key).OrderByDescending(a => a.Key).Select(a => a.Value).FirstOrDefault() : 0;
		if (debug)
		{
			Logger.Debug($"actionId: {actionId} actionIdAdjusted: {actionIdAdjusted} OriginalFunctionManager.GetAdjustedActionId: {OriginalFunctionManager.GetAdjustedActionId(actionId)}");
		}

		return actionIdAdjusted > 0 ? actionIdAdjusted : OriginalFunctionManager.GetAdjustedActionId(actionId);
	}

	public static IDalamudTextureWrap? GetIconTexture(uint? iconId, out bool isOverriden)
	{
		isOverriden = false;
		if (iconId == null)
		{
			return null;
		}

		return Singletons.Get<MediaManager>().GetTextureFromIconIdOverridable((uint) iconId, out isOverriden);
	}

	public static IDalamudTextureWrap? GetActionIconTexture(uint actionId, out bool isOverriden)
	{
		isOverriden = false;
		return GetIconTexture(GetAction(actionId)?.Icon, out isOverriden);
	}

	public static IDalamudTextureWrap? GetStatusIconTexture(uint statusId, out bool isOverriden)
	{
		isOverriden = false;
		return GetIconTexture(GetStatus(statusId)?.Icon, out isOverriden);
	}

	public static LuminaAction? GetAction(uint actionId) => _sheetAction?.GetRow(actionId);
	public static string? GetActionName(uint actionId) => GetAction(actionId)?.Name.ToDalamudString().ToString();
	public static ushort? GetActionIconId(uint actionId) => GetAction(actionId)?.Icon;
	public static LuminaGeneralAction? GetGeneralAction(uint actionId) => _sheetGeneralAction?.GetRow(actionId);
	public static string? GetGeneralActionName(uint actionId) => GetGeneralAction(actionId)?.Name.ToDalamudString().ToString();
	public static int? GetGeneralActionIconId(uint actionId) => GetGeneralAction(actionId)?.Icon;

	public static LuminaStatus? GetStatus(uint statusId) => _sheetStatus?.FirstOrDefault(status => status.RowId.Equals(statusId));

	public static LuminaStatus? GetStatusByAction(uint actionId)
	{
		LuminaAction? action = GetAction(actionId);
		if (action != null && _sheetStatus != null)
		{
			string actionName = action.Name.ToString().ToLower();
			LuminaStatus? status = _sheetStatus.FirstOrDefault(status => status.Name.ToString().ToLower().Equals(actionName));
			if (status != null)
			{
				return status;
			}
		}

		return null;
	}

	public static IBattleChara? GetUnit(Unit unit)
	{
		IPlayerCharacter? player = Services.ClientState.LocalPlayer;
		if (player == null)
		{
			return null;
		}

		IGameObject? target = Services.TargetManager.SoftTarget ?? Services.TargetManager.Target;
		IGameObject? actor = unit switch
		{
			Unit.Player => player,
			Unit.Target => target,
			Unit.TargetOfTarget => Utils.FindTargetOfTarget(player, target, Services.Objects),
			Unit.FocusTarget => Services.TargetManager.FocusTarget,
			_ => null
		};

		if (actor is IBattleChara)
		{
			return (IBattleChara) actor;
		}

		return null;
	}

	public static Status? GetStatus(uint statusId, Unit unit, bool mustMatchPlayerSource = true)
	{
		if (unit == Unit.Any)
		{
			Status? status;
			foreach (Unit unitType in (Unit[]) Enum.GetValues(typeof(Unit)))
			{
				if (unitType != unit)
				{
					status = GetStatus(statusId, unitType, mustMatchPlayerSource);
					if (status != null)
					{
						return status;
					}
				}
			}
		}
		else
		{
			IBattleChara? actor = GetUnit(unit);
			if (actor != null)
			{
				IPlayerCharacter? player = Services.ClientState.LocalPlayer;
				if (player == null)
				{
					return null;
				}

				foreach (Status status in actor.StatusList)
				{
					if (status.StatusId == statusId && (!mustMatchPlayerSource || (mustMatchPlayerSource && status.SourceId == player.GameObjectId)))
					{
						return status;
					}
				}
			}
		}

		return null;
	}

	public static Status? GetStatus(uint[] statusIds, Unit unit, bool mustMatchPlayerSource = true, bool prioritizedByOrder = true)
	{
		if (unit == Unit.Any)
		{
			foreach (Unit unitType in (Unit[]) Enum.GetValues(typeof(Unit)))
			{
				if (unitType != unit)
				{
					Status? status = GetStatus(statusIds, unitType, mustMatchPlayerSource, prioritizedByOrder);
					if (status != null)
					{
						return status;
					}
				}
			}
		}
		else
		{
			IBattleChara? actor = GetUnit(unit);
			if (actor != null)
			{
				IPlayerCharacter? player = Services.ClientState.LocalPlayer;
				if (player == null)
				{
					return null;
				}

				int bestIndex = -1;
				Status? bestStatus = null;

				foreach (Status status in actor.StatusList)
				{
					int foundIndex = Array.IndexOf(statusIds, status.StatusId);
					if (foundIndex > -1 && (!mustMatchPlayerSource || (mustMatchPlayerSource && status.SourceId == player.GameObjectId)) && (bestIndex == -1 || foundIndex < bestIndex))
					{
						bestIndex = foundIndex;
						bestStatus = status;

						if (!prioritizedByOrder)
						{
							return bestStatus;
						}
					}
				}

				return bestStatus;
			}
		}

		return null;
	}
}