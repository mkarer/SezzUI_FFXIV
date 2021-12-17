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

		public unsafe static Status? GetStatus(uint statusId, Enums.Unit unit)
		{
			PlayerCharacter? player = Service.ClientState.LocalPlayer;
			if (player == null) return null;

			GameObject? target = Plugin.TargetManager.SoftTarget ?? Plugin.TargetManager.Target;
			GameObject? actor = unit switch
			{
				Enums.Unit.Player => player,
				Enums.Unit.Target => target,
				Enums.Unit.TargetOfTarget => DelvUI.Helpers.Utils.FindTargetOfTarget(player, target, Plugin.ObjectTable),
				Enums.Unit.FocusTarget => Plugin.TargetManager.FocusTarget,
				_ => null
			};

			if (actor is BattleChara chara)
			{
				foreach (var status in chara.StatusList)
				{
					if (status != null && status.StatusId == statusId && status.SourceID == player.ObjectId)
					{
						return status;
					}
				}
			}

			return null;
		}

		public unsafe static CooldownData GetCooldownData(uint actionId)
		{
			CooldownData data = new();

			PlayerCharacter? player = Service.ClientState.LocalPlayer;
			if (player == null) return data;

			uint actionIdAdjusted = DelvUI.Helpers.SpellHelper.Instance.GetSpellActionId(actionId);

			//if (actionId != actionIdAdjusted)
			//{
			//	LuminaAction? original = GetAction(actionId);
			//	LuminaAction? adj = GetAction(actionIdAdjusted);
			//	string nameorig = (original != null ? original.Name.ToString() : "Unknown");
			//	string namead = (adj != null ? adj.Name.ToString() : "Unknown");

			//	Dalamud.Logging.PluginLog.Debug($"Adjusting action: #{actionId} {nameorig} -> #{actionIdAdjusted} {namead} ");
			//}

			data.ChargesMax = DelvUI.Helpers.SpellHelper.Instance.GetMaxCharges(actionIdAdjusted, player.Level);
			data.ChargesCurrent = DelvUI.Helpers.SpellHelper.Instance.GetStackCount(data.ChargesMax, actionIdAdjusted);
			data.CooldownTotal = DelvUI.Helpers.SpellHelper.Instance.GetRecastTime(actionIdAdjusted);
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
