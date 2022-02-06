#if DEBUG
using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Linq;
using System.Runtime.InteropServices;
using Dalamud.Memory;
using FFXIVClientStructs.Attributes;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Component.GUI;
using SezzUI.Config;
using SezzUI.Enums;
using SezzUI.GameStructs;

namespace SezzUI.Core
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
			Plugin.ChatGui.Print("Okay.");

			try
			{
			}
			catch (Exception ex)
			{
				Logger.Error(ex, "RunTest", $"Error: {ex}");
			}
		}
	}
}
#endif