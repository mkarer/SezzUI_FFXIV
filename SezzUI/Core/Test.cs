using System;
using SezzUI.Helpers;

namespace SezzUI.Core
{
	// ReSharper disable once RedundantUnsafeContext
	public static unsafe class Test
	{
		internal static PluginLogger Logger = new("Test");

		public static void RunTest()
		{
			try
			{
				Plugin.ChatGui.Print("Okay.");
				Plugin.ChatGui.Print(SpellHelper.GetAdjustedActionId(25800u, true).ToString());
				Plugin.ChatGui.Print(SpellHelper.GetAdjustedActionId(7427u, true).ToString());
				Plugin.ChatGui.Print(SpellHelper.GetAdjustedActionId(7428u, true).ToString());
				Plugin.ChatGui.Print(SpellHelper.GetAdjustedActionId(7429u, true).ToString());
			}
			catch (Exception ex)
			{
				Logger.Error(ex, "RunTest", $"Error: {ex}");
			}
		}
	}
}