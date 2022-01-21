using System;
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
using OFM = SezzUI.Hooking.OriginalFunctionManager;

namespace SezzUI.Helpers
{
	public static class SpellHelper
	{
		private static readonly ExcelSheet<LuminaAction>? _sheetAction;
		private static readonly ExcelSheet<LuminaGeneralAction>? _sheetGeneralAction;
		private static readonly ExcelSheet<LuminaStatus>? _sheetStatus;

		static SpellHelper()
		{
			_sheetAction = Plugin.DataManager.Excel.GetSheet<LuminaAction>();
			_sheetGeneralAction = Plugin.DataManager.Excel.GetSheet<LuminaGeneralAction>();
			_sheetStatus = Plugin.DataManager.Excel.GetSheet<LuminaStatus>();
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