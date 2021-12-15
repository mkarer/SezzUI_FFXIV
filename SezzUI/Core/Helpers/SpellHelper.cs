using Lumina.Excel;
using LuminaAction = Lumina.Excel.GeneratedSheets.Action;

namespace SezzUI.Helpers
{
	public static class SpellHelper
	{
		public static LuminaAction? GetAction(uint actionId)
		{
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
	}
}
