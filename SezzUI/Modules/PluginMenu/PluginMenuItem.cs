using System;
using System.IO;
using System.Numerics;
using Dalamud.Logging;
using ImGuiNET;
using ImGuiScene;
using SezzUI.Interface.GeneralElements;

namespace SezzUI.Modules.PluginMenu
{
	public class PluginMenuItem : IDisposable
	{
		public PluginMenuItemConfig Config;
		public Vector2 Size = new(60f, 30f);
		public TextureWrap? Texture;

		public Vector4 Color => !_toggleState ? Config.Color.Vector : Config.ColorToggled.Vector;
		private bool _toggleState = false;
		
		/// <summary>
		/// Recalculate required size upon configuration changes.
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
						Texture = Helpers.ImageCache.Instance.GetImageFromPath(image);
						if (Texture != null)
						{
							contentSize.X = contentSize.Y;
						}
					}
				}
				catch (Exception ex)
				{
					PluginLog.Error(ex, $"[PluginMenuItem] Error reading image ({image}): {ex}");
				}
			}
			else
			{
				string text = Tags.RegexColorTags.IsMatch(Config.Title) ? Tags.RegexColorTags.Replace(Config.Title, "") : Config.Title;
				contentSize = ImGui.CalcTextSize(text);
				contentSize.X += 2 * 8;
			}

			Size.X = contentSize.X;
		}

		public void Toggle()
		{
			if (Config.Toggleable)
			{
				_toggleState = !_toggleState;
			}
		}
		
		public PluginMenuItem(PluginMenuItemConfig config)
		{
			Config = config;
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
				return;
			}
		}
	}
}
