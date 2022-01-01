using System;
using System.Linq;
using Lumina.Excel;
using LuminaAction = Lumina.Excel.GeneratedSheets.Action;
using LuminaStatus = Lumina.Excel.GeneratedSheets.Status;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.ClientState.Statuses;

namespace SezzUI.Helpers
{
	public class CooldownData
	{
		public int ChargesMax = 0;
		public int ChargesCurrent = 0;
		public float CooldownPerCharge = 0;
		public float CooldownTotal = 0;
		public float CooldownTotalElapsed = 0;
		public float CooldownRemaining = 0;
		public float CooldownTotalRemaining = 0;
	}

	public static class SpellHelper
	{
		public static LuminaAction? GetAction(uint actionId)
		{
			// TODO: Level filter?
			ExcelSheet<LuminaAction>? sheet = Plugin.DataManager.GetExcelSheet<LuminaAction>();
			if (sheet != null)
			{
				LuminaAction? action = sheet.GetRow(actionId);
				if (action != null)
				{
					return action;
				}
			}

			return null;
		}

		public static LuminaStatus? GetStatusByAction(uint actionId)
		{
			LuminaAction? action = GetAction(actionId);
			if (action != null)
			{
				ExcelSheet<LuminaStatus>? sheet = Plugin.DataManager.GetExcelSheet<LuminaStatus>();

				if (sheet != null)
				{
					string actionName = action.Name.ToString().ToLower();
					LuminaStatus? status = sheet.FirstOrDefault(status => status.Name.ToString().ToLower().Equals(actionName));
					if (status != null)
					{
						return status;
					}
				}
			}

			return null;
		}

		public static LuminaStatus? GetStatus(uint statusId)
		{
			ExcelSheet<LuminaStatus>? sheet = Plugin.DataManager.GetExcelSheet<LuminaStatus>();

			if (sheet != null)
			{
				return sheet.FirstOrDefault(status => status.RowId.Equals(statusId));
			}

			return null;
		}

		public static unsafe BattleChara? GetUnit(Enums.Unit unit)
		{
			PlayerCharacter? player = Plugin.ClientState.LocalPlayer;
            if (player == null) { return null; }

			GameObject? target = Plugin.TargetManager.SoftTarget ?? Plugin.TargetManager.Target;
			GameObject? actor = unit switch
			{
				Enums.Unit.Player => player,
				Enums.Unit.Target => target,
				Enums.Unit.TargetOfTarget => DelvUI.Helpers.Utils.FindTargetOfTarget(player, target, Plugin.ObjectTable),
				Enums.Unit.FocusTarget => Plugin.TargetManager.FocusTarget,
				_ => null
			};

			if (actor is BattleChara)
			{
				return (BattleChara)actor;
			}

			return null;
		}

        public static unsafe Status? GetStatus(uint statusId, Enums.Unit unit, bool mustMatchPlayerSource = true)
		{
			if (unit == Enums.Unit.Any)
			{
				Status? status;
				foreach (Enums.Unit unitType in (Enums.Unit[])Enum.GetValues(typeof(Enums.Unit)))
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
                    if (player == null) { return null; }

					foreach (var status in actor.StatusList)
					{
						if (status != null && status.StatusId == statusId && (!mustMatchPlayerSource || (mustMatchPlayerSource && status.SourceID == player.ObjectId)))
						{
							return status;
						}
					}
				}
			}

			return null;
		}

        public static unsafe Status? GetStatus(uint[] statusIds, Enums.Unit unit, bool mustMatchPlayerSource = true)
		{
			if (unit == Enums.Unit.Any)
			{
				Status? status;
				foreach (Enums.Unit unitType in (Enums.Unit[])Enum.GetValues(typeof(Enums.Unit)))
				{
					if (unitType != unit)
					{
						status = GetStatus(statusIds, unitType, mustMatchPlayerSource);
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
                    if (player == null) { return null; }

					foreach (var status in actor.StatusList)
					{
						if (status != null && Array.IndexOf(statusIds, status.StatusId) > -1 && (!mustMatchPlayerSource || (mustMatchPlayerSource && status.SourceID == player.ObjectId)))
                        {
							return status;
						}
					}
				}
			}

			return null;
		}

        public static unsafe CooldownData GetCooldownData(uint actionId)
		{
			CooldownData data = new();

			PlayerCharacter? player = Plugin.ClientState.LocalPlayer;
            if (player == null) { return data; }

			uint actionIdAdjusted = DelvUI.Helpers.SpellHelper.Instance.GetSpellActionId(actionId);
            ushort maxLevelCharges = DelvUI.Helpers.SpellHelper.Instance.GetMaxCharges(actionIdAdjusted, Plugin.MAX_PLAYER_LEVEL);

            data.ChargesMax = player.Level < Plugin.MAX_PLAYER_LEVEL ? DelvUI.Helpers.SpellHelper.Instance.GetMaxCharges(actionIdAdjusted, player.Level) : maxLevelCharges;
			data.ChargesCurrent = DelvUI.Helpers.SpellHelper.Instance.GetStackCount(maxLevelCharges, actionIdAdjusted);
			data.CooldownTotal = DelvUI.Helpers.SpellHelper.Instance.GetRecastTime(actionIdAdjusted) / maxLevelCharges * data.ChargesMax;
			data.CooldownTotalElapsed = DelvUI.Helpers.SpellHelper.Instance.GetRecastTimeElapsed(actionIdAdjusted);
			data.CooldownPerCharge = data.CooldownTotal / data.ChargesMax;

			float remaining = (data.CooldownTotal - data.CooldownTotalElapsed) % data.CooldownPerCharge;
			if (remaining > 0)
			{
				data.CooldownRemaining = remaining;
			}

			return data;
		}
	}
}
