// Stolen from goat's Wotsit
// https://github.com/goaaats/Dalamud.FindAnything/blob/master/Dalamud.FindAnything/DalamudReflector.cs
// https://stackoverflow.com/questions/1565734/is-it-possible-to-set-private-property-via-reflection

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using Dalamud.Plugin;
using Dalamud.Utility;
using ImGuiNET;
using SezzUI.Logging;

namespace SezzUI.Helper
{
	public static class DalamudHelper
	{
		internal static T GetService<T>() => (T) typeof(IDalamudPlugin).Assembly.GetType("Dalamud.Service`1")!.MakeGenericType(typeof(T)).GetMethod("Get", BindingFlags.Static | BindingFlags.Public)!.Invoke(null, null)!;
		internal static object GetService(string name) => typeof(IDalamudPlugin).Assembly.GetType("Dalamud.Service`1")!.MakeGenericType(typeof(IDalamudPlugin).Assembly.GetType(name)!).GetMethod("Get", BindingFlags.Static | BindingFlags.Public)!.Invoke(null, null)!;

		public struct PluginEntry
		{
			public string Name;
			public bool Enabled;
		}

		public static IReadOnlyList<PluginEntry> Plugins { get; private set; }
		internal static PluginLogger Logger;

		public static string AssetDirectory => GetService("Dalamud.DalamudStartInfo").GetPropertyValue<string>("AssetDirectory");

		public static (string?, uint?, ImFontPtr? imFontPtr) GetDefaultFont()
		{
			// At the time of implementing this the default font is: NotoSansCJKjp-Medium.otf (17px)
			string assetDirectory = null!;
			try
			{
				assetDirectory = AssetDirectory;
			}
			catch (Exception ex)
			{
				Logger.Error($"Error getting asset directory: {ex}");
			}

			ImGuiIOPtr io = ImGui.GetIO();
			if (!assetDirectory.IsNullOrEmpty() && io.Fonts.Fonts.Size > 0)
			{
				ImFontPtr defaultFont = io.Fonts.Fonts[0];
				string defaultFontName = defaultFont.GetDebugName();
				//Logger.Debug("GetDefaultFont", $"ImGui Default Font[0]: {defaultFontName}");

				Match dalamudFontMatch = Regex.Match(defaultFontName, @"^(.*), ([1-9]\d*(\.)\d*|0?(\.)\d*[1-9]\d*|[1-9]\d*)px$");
				if (dalamudFontMatch.Success && uint.TryParse(dalamudFontMatch.Groups[2].Value, out uint dalamudFontSize)) // 17
				{
					string dalamudFontFile = dalamudFontMatch.Groups[1].Value; // NotoSansCJKjp-Medium.otf
					//Logger.Debug("GetDefaultFont", $"Dalamud Font: {dalamudFontFile} Size: {dalamudFontSize}px");
					return (Path.Combine(assetDirectory, "UIRes", dalamudFontFile), dalamudFontSize, defaultFont);
				}
			}

			return (null, null, null);
		}

		public static void RefreshPlugins()
		{
			object pluginManager = GetService("Dalamud.Plugin.Internal.PluginManager");
			IEnumerable<object> pluginList = pluginManager.GetPropertyValue<IEnumerable<object>>("InstalledPlugins"); // LocalPlugin

			List<PluginEntry> list = new();
			foreach (object plugin in pluginList)
			{
				if (plugin.GetType().GetProperty("DalamudInterface", BindingFlags.Public | BindingFlags.Instance)!.GetValue(plugin) == null)
				{
					continue;
				}

				try
				{
					string name = plugin.GetPropertyValue<string>("Name");
					bool loaded = plugin.GetPropertyValue<bool>("IsLoaded");
					if (loaded)
					{
						bool enabled = true; // Assume that all unsupported plugins are enabled

						switch (name)
						{
							case "TextAdvance":
								enabled = plugin.GetFieldValue<IDalamudPlugin>("instance").GetFieldValue<bool>("Enabled");
								//Logger.Debug("RefreshPlugins", $"Plugin: {name} Enabled: {enabled}");
								break;
						}

						//Logger.Debug("RefreshPlugins", $"Plugin: {name} Enabled: {enabled}");
						PluginEntry entry = new()
						{
							Name = name,
							Enabled = enabled
						};

						list.Add(entry);
					}
				}
				catch (Exception ex)
				{
					Logger.Error(ex);
				}
			}

			Plugins = list;
		}

		static DalamudHelper()
		{
			Logger = new("DalamudHelper");
			Plugins = new List<PluginEntry>();
		}
	}
}