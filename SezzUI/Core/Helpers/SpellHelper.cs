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

        public static unsafe Status? GetStatus(uint[] statusIds, Enums.Unit unit, bool mustMatchPlayerSource = true, bool priorizedByOrder = true)
		{
			if (unit == Enums.Unit.Any)
			{
				Status? status;
				foreach (Enums.Unit unitType in (Enums.Unit[])Enum.GetValues(typeof(Enums.Unit)))
				{
					if (unitType != unit)
					{
						status = GetStatus(statusIds, unitType, mustMatchPlayerSource, priorizedByOrder);
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

                    int bestIndex = -1;
                    Status? bestStatus = null;

					foreach (var status in actor.StatusList)
					{
                        if (status != null)
                        {
                            int foundIndex = Array.IndexOf(statusIds, status.StatusId);
                            if (foundIndex > -1 && (!mustMatchPlayerSource || (mustMatchPlayerSource && status.SourceID == player.ObjectId)) && (bestIndex == -1 || foundIndex < bestIndex))
                            {
                                bestIndex = foundIndex;
                                bestStatus = status;

                                if (!priorizedByOrder)
                                {
                                    return bestStatus;
                                }
                            }
                        }
					}

                    return bestStatus;
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
            ushort maxChargesMaxLevel = DelvUI.Helpers.SpellHelper.Instance.GetMaxCharges(actionIdAdjusted, Plugin.MAX_PLAYER_LEVEL);
            ushort maxChargesCurrentLevel = player.Level < Plugin.MAX_PLAYER_LEVEL ? DelvUI.Helpers.SpellHelper.Instance.GetMaxCharges(actionIdAdjusted, player.Level) : maxChargesMaxLevel;
            float chargesMod = maxChargesCurrentLevel > 1 ? 1f / maxChargesMaxLevel * maxChargesCurrentLevel : 1;

            data.ChargesMax = maxChargesCurrentLevel;
            data.CooldownTotal = DelvUI.Helpers.SpellHelper.Instance.GetRecastTime(actionIdAdjusted) * chargesMod; // GetRecastTime returns total cooldown for max level charges (but only if we have multiple charges right now?)

            if (data.CooldownTotal > 0)
            {
                data.CooldownPerCharge = data.CooldownTotal / maxChargesCurrentLevel;
                data.CooldownTotalElapsed = Math.Min(data.CooldownTotal, DelvUI.Helpers.SpellHelper.Instance.GetRecastTimeElapsed(actionIdAdjusted)); // GetRecastTimeElapsed is weird when maxChargesCurrentLevel differs from maxChargesMaxLevel
                data.CooldownTotalRemaining = (data.CooldownTotal - data.CooldownTotalElapsed);
                data.CooldownRemaining = data.CooldownTotalRemaining % data.CooldownPerCharge;
            }

            data.ChargesCurrent = data.CooldownTotalElapsed > 0 ? (int)Math.Floor(data.CooldownTotalElapsed / data.CooldownPerCharge) : data.ChargesMax;

            //if (actionId == 3640)
            //{
            //    Dalamud.Logging.PluginLog.Debug($"CooldownTotal {data.CooldownTotal} CooldownRemaining {data.CooldownRemaining} chargesMod {chargesMod} " +
            //        $"GetRecastTime {DelvUI.Helpers.SpellHelper.Instance.GetRecastTime(actionIdAdjusted)} GetRecastTimeElapsed {DelvUI.Helpers.SpellHelper.Instance.GetRecastTimeElapsed(actionIdAdjusted)}");
            //}

            return data;
		}
	}
}
