using System;
using System.Linq;
using Lumina.Excel;
using LuminaAction = Lumina.Excel.GeneratedSheets.Action;
using LuminaStatus = Lumina.Excel.GeneratedSheets.Status;
using Dalamud.Game.ClientState.Objects.SubKinds;

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
			if (sheet is not null)
			{
				LuminaAction? action = sheet.GetRow(actionId);
				if (action is not null)
				{
					return action;
				}
			}

			return null;
		}

		public static LuminaStatus? GetStatusByAction(uint actionId)
		{
			LuminaAction? action = GetAction(actionId);
			if (action is not null)
			{
				ExcelSheet<LuminaStatus>? sheet = Plugin.DataManager.GetExcelSheet<LuminaStatus>();

				if (sheet is not null)
				{
					string actionName = action.Name.ToString().ToLower();
					LuminaStatus? status = sheet.FirstOrDefault(status => status.Name.ToString().ToLower().Equals(actionName));
					if (status is not null)
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
			if (player != null)
			{
				uint actionIdAdjusted = DelvUI.Helpers.SpellHelper.Instance.GetSpellActionId(actionId);

				data.ChargesMax = DelvUI.Helpers.SpellHelper.Instance.GetMaxCharges(actionIdAdjusted, player.Level);
				data.ChargesCurrent = DelvUI.Helpers.SpellHelper.Instance.GetStackCount(data.ChargesMax, actionIdAdjusted);
				data.CooldownTotal = DelvUI.Helpers.SpellHelper.Instance.GetRecastTime(actionIdAdjusted);
				data.CooldownTotalElapsed = DelvUI.Helpers.SpellHelper.Instance.GetRecastTimeElapsed(actionIdAdjusted);
				data.CooldownPerCharge = data.CooldownTotal / data.ChargesMax;

				// TODO: Get correct data by level...
				float remaining = (data.CooldownTotal - data.CooldownTotalElapsed) % data.CooldownPerCharge;
				if (remaining > 0)
				{
					data.CooldownRemaining = remaining;
				}
			}

			return data;
		}
	}
}
