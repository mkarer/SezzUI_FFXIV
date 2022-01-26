using System;
using System.IO;
using System.Linq;
using System.Numerics;
using Dalamud.Logging;
using ImGuiNET;
using ImGuiScene;
using SezzUI.Helpers;
using SezzUI.Interface.GeneralElements;

namespace SezzUI.Modules.PluginMenu
{
	public class PluginMenuItem : IDisposable
	{
		public PluginMenuItemConfig Config;
		public Vector2 Size = new(60f, 30f);
		public TextureWrap? Texture;

		public Vector4 Color => !_toggleState ? Config.Color.Vector : Config.ColorToggled.Vector;
		private bool _toggleState;

		/// <summary>
		///     Recalculate required size upon configuration changes.
		/// </summary>
		public void Update()
		{
			if (!Config.Enabled)
			{
				return;
			}

			Texture = null;
			Vector2 contentSize = new(16f, Size.Y);

			if (Config.Title.StartsWith("::") && Config.Title.Length > 2)
			{
				string image = Plugin.AssemblyLocation + "Media\\" + Config.Title.Substring(2);
				try
				{
					if (File.Exists(image))
					{
						Texture = ImageCache.Instance.GetImageFromPath(image);
						if (Texture != null)
						{
							contentSize.X = contentSize.Y;
						}
					}
				}
				catch (Exception ex)
				{
					PluginLog.Error(ex, $"[PluginMenuItem::Update] Error reading image ({image}): {ex}");
				}
			}
			else
			{
				string text = Tags.RegexColorTags.IsMatch(Config.Title) ? Tags.RegexColorTags.Replace(Config.Title, "") : Config.Title;
				contentSize = ImGui.CalcTextSize(text);
				contentSize.X += 2 * 8;
			}

			Size.X = contentSize.X;
			Toggle(GetPluginToggleState(false));
		}

		public void Toggle()
		{
			if (Config.Toggleable)
			{
				Toggle(GetPluginToggleState(true) ?? !_toggleState);
			}
		}

		private void Toggle(bool? state)
		{
			if (state != null)
			{
				_toggleState = (bool) state;
			}
		}

		private bool? GetPluginToggleState(bool invert)
		{
			if (Config.PluginToggleName != "" && Config.PluginToggleProperty != "")
			{
				try
				{
					DalamudHelper.RefreshPlugins();
					DalamudHelper.PluginEntry? plugin = DalamudHelper.Plugins.FirstOrDefault(plugin => plugin.Name == Config.PluginToggleName);
					PluginLog.Debug($"enabled{!plugin?.Enabled}");
					return invert ? !plugin?.Enabled ?? null : plugin?.Enabled ?? null;
				}
				catch (Exception ex)
				{
					PluginLog.Error(ex, $"[PluginMenuItem::GetPluginToggleState] Error: {ex}");
				}
			}

			return null;
		}

		public PluginMenuItem(PluginMenuItemConfig config)
		{
			Config = config;
			Toggle(GetPluginToggleState(false));
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		~PluginMenuItem()
		{
			Dispose(false);
		}

		private void Dispose(bool disposing)
		{
			if (!disposing)
			{
			}
		}
	}
}