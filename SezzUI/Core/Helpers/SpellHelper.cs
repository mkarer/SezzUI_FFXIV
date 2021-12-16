using System.Linq;
using Lumina.Excel;
using LuminaAction = Lumina.Excel.GeneratedSheets.Action;
using LuminaStatus = Lumina.Excel.GeneratedSheets.Status;

namespace SezzUI.Helpers
{
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
	}
}
