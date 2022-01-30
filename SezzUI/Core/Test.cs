using System;
using System.Runtime.InteropServices;
using FFXIVClientStructs.FFXIV.Component.GUI;
using SezzUI.Enums;

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
			try
			{
				Plugin.ChatGui.Print("Okay.");

				AtkStage* stage = AtkStage.GetSingleton();
				if (stage == null)
				{
					return;
				}

				AtkUnitList* loadedUnitsList = &stage->RaptureAtkUnitManager->AtkUnitManager.AllLoadedUnitsList;
				if (loadedUnitsList == null)
				{
					return;
				}

				AtkUnitBase** addonList = &loadedUnitsList->AtkUnitEntries;
				if (addonList == null)
				{
					return;
				}

				for (int i = 0; i < loadedUnitsList->Count; i++)
				{
					AtkUnitBase* addon = addonList[i];
					if (addon == null || addon->RootNode == null || addon->UldManager.LoadedState != 3)
					{
						continue;
					}

					string? name = Marshal.PtrToStringAnsi(new(addon->Name));
					if (name == null)
					{
						continue;
					}

					byte visibilityFlag = *((byte*) addon + 0x1B6);
					Logger.Debug("RunTest", $"Addon: {ToFixedLength(name, 24)} | Visibility: {visibilityFlag} {ToBinaryString(visibilityFlag)} ({(AddonVisibility)visibilityFlag})");
				}
			}
			catch (Exception ex)
			{
				Logger.Error(ex, "RunTest", $"Error: {ex}");
			}
		}
	}
}