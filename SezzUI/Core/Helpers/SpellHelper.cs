using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.ClientState.Statuses;
using Dalamud.Utility;
using DelvUI.Helpers;
using Lumina.Excel;
using SezzUI.Enums;
using LuminaAction = Lumina.Excel.GeneratedSheets.Action;
using LuminaGeneralAction = Lumina.Excel.GeneratedSheets.GeneralAction;
using LuminaStatus = Lumina.Excel.GeneratedSheets.Status;
using SezzUI.Hooking;

namespace SezzUI.Helpers
{
	public static class SpellHelper
	{
		private static readonly ExcelSheet<LuminaAction>? _sheetAction;
		private static readonly ExcelSheet<LuminaGeneralAction>? _sheetGeneralAction;
		private static readonly ExcelSheet<LuminaStatus>? _sheetStatus;
		private static readonly Dictionary<uint, Dictionary<uint, uint>> _actionAdjustments;

		static SpellHelper()
		{
			_sheetAction = Plugin.DataManager.Excel.GetSheet<LuminaAction>();
			_sheetGeneralAction = Plugin.DataManager.Excel.GetSheet<LuminaGeneralAction>();
			_sheetStatus = Plugin.DataManager.Excel.GetSheet<LuminaStatus>();
			_actionAdjustments = new()
			{
				// Hardcoded values for GetAdjustedActionId for actions that might get replaced by
				// another plugin (combo plugins hook GetAdjustedActionId):

				// RPR
				{24405, new() {{72, 24405}}}, // Arcane Circle
				{24394, new() {{80, 24394}}}, // Enshroud
				// PLD
				{3538, new() {{54, 3538}}}, // Goring Blade
				{7383, new() {{54, 7383}}}, // Requiescat
				// MCH
				{2864, new() {{40, 2864}, {80, 16501}}}, // Rook Autoturret/Automation Queen
				// BLM
				{3573, new() {{52, 3573}}}, // Ley Lines
				// DNC
				{15997, new() {{15, 15997}}}, // Standard Step
				{15998, new() {{70, 15998}}}, // Technical Step
				// SGE
				{24293, new() {{30, 24293}, {72, 24308}, {82, 24314}}}, // Eukrasian Dosis
				// SMN
				{3581, new() {{58, 3581}, {70, 7427}}},
				// BRD
				{3559, new() {{52, 3559}}}, // The Wanderer's Minuet
				// GNB
				{16161, new() {{68, 16161}, {82, 25758}}}, // Heart of Stone/Heart of Corundum
				// DRG
				//{88, new() {{50, 88}, {86, 25772}}}, // Chaos Thrust (XIVCombo)
				{3555, new() {{60, 3555}}} // Geirskogul
			};
		}

		public static uint GetAdjustedActionId(uint actionId)
		{
			byte level = Plugin.ClientState.LocalPlayer?.Level ?? 0;
			uint actionIdAdjusted = _actionAdjustments.TryGetValue(actionId, out Dictionary<uint, uint>? actionAdjustments) ? actionAdjustments.Where(a => level >= a.Key).OrderByDescending(a => a.Key).Select(a => a.Value).FirstOrDefault() : 0;
			return actionIdAdjusted > 0 ? actionIdAdjusted : OriginalFunctionManager.GetAdjustedActionId(actionId);
		}

		public static LuminaAction? GetAction(uint actionId) => _sheetAction?.GetRow(actionId);
		public static string? GetActionName(uint actionId) => GetAction(actionId)?.Name.ToDalamudString().ToString();
		public static ushort? GetActionIcon(uint actionId) => GetAction(actionId)?.Icon;
		public static LuminaGeneralAction? GetGeneralAction(uint actionId) => _sheetGeneralAction?.GetRow(actionId);
		public static string? GetGeneralActionName(uint actionId) => GetGeneralAction(actionId)?.Name.ToDalamudString().ToString();
		public static int? GetGeneralActionIcon(uint actionId) => GetGeneralAction(actionId)?.Icon;

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

		public static BattleChara? GetUnit(Unit unit)
		{
			PlayerCharacter? player = Plugin.ClientState.LocalPlayer;
			if (player == null)
			{
				return null;
			}

			GameObject? target = Plugin.TargetManager.SoftTarget ?? Plugin.TargetManager.Target;
			GameObject? actor = unit switch
			{
				Unit.Player => player,
				Unit.Target => target,
				Unit.TargetOfTarget => Utils.FindTargetOfTarget(player, target, Plugin.ObjectTable),
				Unit.FocusTarget => Plugin.TargetManager.FocusTarget,
				_ => null
			};

			if (actor is BattleChara)
			{
				return (BattleChara) actor;
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
				BattleChara? actor = GetUnit(unit);
				if (actor != null)
				{
					PlayerCharacter? player = Plugin.ClientState.LocalPlayer;
					if (player == null)
					{
						return null;
					}

					foreach (Status status in actor.StatusList)
					{
						if (status.StatusId == statusId && (!mustMatchPlayerSource || mustMatchPlayerSource && status.SourceID == player.ObjectId))
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
				BattleChara? actor = GetUnit(unit);
				if (actor != null)
				{
					PlayerCharacter? player = Plugin.ClientState.LocalPlayer;
					if (player == null)
					{
						return null;
					}

					int bestIndex = -1;
					Status? bestStatus = null;

					foreach (Status status in actor.StatusList)
					{
						int foundIndex = Array.IndexOf(statusIds, status.StatusId);
						if (foundIndex > -1 && (!mustMatchPlayerSource || mustMatchPlayerSource && status.SourceID == player.ObjectId) && (bestIndex == -1 || foundIndex < bestIndex))
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
}