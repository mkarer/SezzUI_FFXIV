#if DEBUG
using System;
using SezzUI.Logging;

namespace SezzUI.Modules.Test
{
	// ReSharper disable once RedundantUnsafeContext
	public static unsafe class Test
	{
		internal static PluginLogger Logger = new("Test");

		private static string ToFixedLength(string input, int length)
		{
			if (input.Length > length)
			{
				return input.Substring(0, length);
			}

			return input.PadRight(length, ' ');
		}

		private static string ToBinaryString(byte number) => Convert.ToString(number, 2).PadLeft(8, '0');

		public static void RunTest()
		{
			Service.ChatGui.Print("Okay.");

			try
			{
			}
			catch (Exception ex)
			{
				Logger.Error(ex);
			}
		}
	}
}
#endif