using System;
using System.Linq;
using System.Numerics;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Utility;
using ImGuiNET;
using SezzUI.Helper;
using SezzUI.Logging;

namespace SezzUI.Modules.PluginMenu;

public class PluginMenuItem : IDisposable
{
	public PluginMenuItemConfig Config;
	public Vector2 Size = new(60f, 30f);
	public bool HasTexture => Config.Title.StartsWith("::") && Config.Title.Length > 2;
	internal PluginLogger Logger;

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

		IDalamudTextureWrap? texture = Texture; // Request texture and calculate size
		Toggle(GetPluginToggleState(false));
	}

	public IDalamudTextureWrap? Texture
	{
		get
		{
			IDalamudTextureWrap? texture = null;
			Vector2 contentSize = new(16f, Size.Y);

			if (HasTexture)
			{
				string? file = ThreadSafety.IsMainThread ? Singletons.Get<MediaManager>().GetIconFileName(Config.Title.Substring(2)) : null;
				texture = file != null ? Services.TextureProvider.GetFromFile(file!).GetWrapOrEmpty() : null;
				if (texture != null && texture.ImGuiHandle != IntPtr.Zero)
				{
					contentSize.X = contentSize.Y;
				}
			}
			else
			{
				string text = Tags.RegexColorTags.IsMatch(Config.Title) ? Tags.RegexColorTags.Replace(Config.Title, "") : Config.Title;
				contentSize = ImGui.CalcTextSize(text);
				contentSize.X += 2 * 8;
			}

			Size.X = contentSize.X;
			return texture;
		}
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
				return invert ? !plugin?.Enabled ?? null : plugin?.Enabled ?? null;
			}
			catch (Exception ex)
			{
				Logger.Error($"[PluginMenuItem::GetPluginToggleState] Error: {ex}");
			}
		}

		return null;
	}

	public PluginMenuItem(PluginMenuItemConfig config)
	{
		Logger = new(GetType().Name);
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