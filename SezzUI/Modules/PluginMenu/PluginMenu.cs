using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;
using SezzUI.Configuration;
using SezzUI.Enums;
using SezzUI.Helper;
using SezzUI.Interface;

namespace SezzUI.Modules.PluginMenu;

public class PluginMenu : PluginModule
{
	private PluginMenuConfig Config => (PluginMenuConfig) _config;
#if DEBUG
	private readonly PluginMenuDebugConfig _debugConfig;
#endif
	private readonly List<PluginMenuItem> _items;
	private static long _lastError;
	private const byte BORDER_SIZE = 1;
	private const byte BUTTON_PADDING = 4;

	public Vector2 Size = Vector2.Zero;
	public readonly Vector2 SizePreview = new(300f, 30f);

	private bool _forceUpdate = true;

	protected override unsafe void OnDraw(DrawState drawState)
	{
		if (drawState != DrawState.Visible && drawState != DrawState.Partially)
		{
			return;
		}

		if (_forceUpdate)
		{
			UpdateItems();
		}
		else
		{
			// TODO: We should only need to resize after a texture is loaded
			UpdateSize();
		}

		List<PluginMenuItem> enabledItems = _items.Where(item => item.Config.Enabled).ToList();
		if (enabledItems.Count() == 0)
		{
			return;
		}

		IntPtr nowLoading = Services.GameGui.GetAddonByName("NowLoading");
		AtkResNode* nowLoadingNode = ((AtkUnitBase*) nowLoading)->RootNode;
		float opacity = !nowLoadingNode->IsVisible() ? 1f : Math.Min(160f, 160 - nowLoadingNode->Alpha_2) / 160f; // At about 172 the _NaviMap is hidden here.
		if (opacity <= 0)
		{
			return;
		}

		bool rightToLeft = Config.Anchor is DrawAnchor.Right or DrawAnchor.TopRight or DrawAnchor.BottomRight;
		Vector2 menuPos = DrawHelper.GetAnchoredPosition(Size, Config.Anchor) + Config.Position;

		// Buttons
		ImGui.SetNextWindowSize(Size, ImGuiCond.Always);
		ImGui.SetNextWindowContentSize(Size);
		ImGui.SetNextWindowPos(menuPos);
		ImGuiHelper.PushButtonStyle(BORDER_SIZE, opacity); // Would clip some borders otherwise...

		DrawHelper.DrawInWindow("SezzUI_PluginMenu_Buttons", menuPos, Size, true, false, drawList =>
		{
			float buttonOffset = rightToLeft ? Size.X - enabledItems[0].Size.X - BORDER_SIZE : BORDER_SIZE; // Need to add border size here for R2L or it will clip the right border. 
			uint shadowColor = ImGui.ColorConvertFloat4ToU32(new(0, 0, 0, opacity));

			for (int i = 0; i < enabledItems.Count(); i++)
			{
				PluginMenuItem item = enabledItems[i];
				ImGui.SameLine(buttonOffset, 0f);

				// Button
				if (ImGui.Button($"##SezzUI_PMB{i}", item.Size))
				{
#if DEBUG
					if (_debugConfig.LogGeneral)
					{
						Logger.Debug($"Menu item clicked: #{i}");
					}
#endif
					item.Toggle();
					if (item.Config.Command.StartsWith("/"))
					{
#if DEBUG
						if (_debugConfig.LogGeneral)
						{
							Logger.Debug($"Executing command: {item.Config.Command}");
						}
#endif
						ChatHelper.SendMessage(item.Config.Command);
					}
				}

				// Content
				Vector4 color = item.Color.AddTransparency(opacity);
				Vector2 buttonPos = new(menuPos.X + buttonOffset, menuPos.Y);

				if (item.Texture != null)
				{
					Vector2 imageSize = new(item.Size.X - 8, item.Size.Y - 8);
					Vector2 imagePos = DrawHelper.GetAnchoredPosition(buttonPos, item.Size, imageSize, DrawAnchor.Center);
					imagePos.Y += BORDER_SIZE;
					drawList.AddImage(item.Texture.ImGuiHandle, imagePos, imagePos + imageSize, Vector2.Zero, Vector2.One, ImGui.ColorConvertFloat4ToU32(color.AddTransparency(opacity)));
				}
				else if (Tags.RegexColorTags.IsMatch(item.Config.Title))
				{
					string cleanTitle = Tags.RegexColorTags.Replace(item.Config.Title, "");
					Vector2 cleanTitleSize = ImGui.CalcTextSize(cleanTitle);
					Vector2 textPosition = DrawHelper.GetAnchoredPosition(buttonPos, item.Size, cleanTitleSize, DrawAnchor.Center);
					textPosition.Y += 2;

					MatchCollection matches = Tags.RegexColor.Matches(item.Config.Title);
					try
					{
						foreach (Match match in matches)
						{
							if (match.Groups[1].Success)
							{
								// Color
								color.X = int.Parse(match.Groups[1].Value[4..6], NumberStyles.HexNumber) / 255f;
								color.Y = int.Parse(match.Groups[1].Value[6..8], NumberStyles.HexNumber) / 255f;
								color.Z = int.Parse(match.Groups[1].Value[8..10], NumberStyles.HexNumber) / 255f;
								color.W = int.Parse(match.Groups[1].Value[2..4], NumberStyles.HexNumber) / 255f * opacity;
							}
							else if (match.Groups[2].Success)
							{
								// Text
								DrawHelper.DrawShadowText(match.Groups[2].Value, textPosition, ImGui.ColorConvertFloat4ToU32(color), shadowColor, drawList);
								textPosition.X += ImGui.CalcTextSize(match.Groups[2].Value).X;
							}
						}
					}
					catch (Exception ex)
					{
						long now = Environment.TickCount64;
						if (now - _lastError > 5000)
						{
							_lastError = now;
							Logger.Error(ex);
						}
					}
				}
				else
				{
					DrawHelper.DrawCenteredShadowText(item.Config.Title, new(buttonPos.X, buttonPos.Y + 1), item.Size, ImGui.ColorConvertFloat4ToU32(color.AddTransparency(opacity)), shadowColor, drawList);
				}

				// Tooltip
				if (item.Config.Tooltip != "" && ImGui.IsMouseHoveringRect(buttonPos, buttonPos + item.Size))
				{
					Singletons.Get<TooltipsHelper>().ShowTooltipOnCursor(item.Config.Tooltip);
				}

				if (i + 1 < enabledItems.Count())
				{
					buttonOffset += rightToLeft ? -enabledItems[i + 1].Size.X - BUTTON_PADDING : item.Size.X + BUTTON_PADDING;
				}
			}
		});

		ImGuiHelper.PopButtonStyle();
	}

	protected override void OnEnable()
	{
		Singletons.Get<MediaManager>().PathChanged += OnMediaPathChanged;
		Singletons.Get<MediaManager>().FontAssignmentsChanged += OnFontAssignmentsChanged;
		//Services.PluginInterface.UiBuilder.BuildFonts += OnBuildFonts;

		_forceUpdate = true;
		UpdateItems();
	}

	protected override void OnDisable()
	{
		Singletons.Get<MediaManager>().PathChanged -= OnMediaPathChanged;
		Singletons.Get<MediaManager>().FontAssignmentsChanged -= OnFontAssignmentsChanged;
		//Services.PluginInterface.UiBuilder.BuildFonts -= OnBuildFonts;
	}

	private void UpdateItems()
	{
		if ((this as IPluginComponent).IsEnabled && Plugin.DrawState != DrawState.Unknown)
		{
			// ImGui isn't ready while DrawState is Unknown and will crash if we try to calculate item text sizes!
			_items.ForEach(item => item.Update());
			UpdateSize();
			_forceUpdate = false;
		}
	}

	private void UpdateItemsDelayed()
	{
		_forceUpdate = true;
	}

	private void OnMediaPathChanged(string path) => UpdateItemsDelayed();
	private void OnFontAssignmentsChanged() => UpdateItemsDelayed();
	private void OnBuildFonts() => UpdateItemsDelayed();

	private void UpdateSize()
	{
		Size = Vector2.Zero;
		List<PluginMenuItem> enabledItems = _items.Where(item => item.Config.Enabled).ToList();
		if (enabledItems.Count() > 0)
		{
			Size = new(enabledItems.Sum(item => item.Size.X) + 2 * BORDER_SIZE + (enabledItems.Count() - 1) * BUTTON_PADDING, enabledItems[0].Size.Y + 2 * BORDER_SIZE);
		}
	}

	public PluginMenu(AnchorablePluginConfigObject config) : base(config)
	{
#if DEBUG
		_debugConfig = Singletons.Get<ConfigurationManager>().GetConfigObject<PluginMenuDebugConfig>();
#endif
		Config.ValueChangeEvent += OnConfigPropertyChanged;
		Config.Items.ForEach(x => x.ValueChangeEvent += OnConfigPropertyChanged);

		Singletons.Get<ConfigurationManager>().Reset += OnConfigReset;
		_items = new() {new(Config.Item1), new(Config.Item2), new(Config.Item3), new(Config.Item4), new(Config.Item5), new(Config.Item6), new(Config.Item7), new(Config.Item8), new(Config.Item9), new(Config.Item10)};

		DraggableElements.Add(new PluginMenuDraggableHudElement(this, "Plugin Menu"));
		(this as IPluginComponent).SetEnabledState(Config.Enabled);
	}

	#region Configuration Events

	private void OnConfigPropertyChanged(object sender, OnChangeBaseArgs args)
	{
		_lastError = 0;

		if (sender is PluginMenuItemConfig)
		{
			UpdateItems();
			return;
		}

		switch (args.PropertyName)
		{
			case "Enabled":
#if DEBUG
				if (_debugConfig.LogConfigurationManager)
				{
					Logger.Debug($"{args.PropertyName}: {Config.Enabled}");
				}
#endif
				(this as IPluginComponent).SetEnabledState(Config.Enabled);
				break;
		}
	}

	private void OnConfigReset(ConfigurationManager sender, PluginConfigObject config)
	{
		if (config != _config)
		{
			return;
		}

		_lastError = 0;
#if DEBUG
		if (_debugConfig.LogConfigurationManager)
		{
			Logger.Debug("Resetting...");
		}
#endif
		(this as IPluginComponent).Disable();
#if DEBUG
		if (_debugConfig.LogConfigurationManager)
		{
			Logger.Debug($"Config.Enabled: {Config.Enabled}");
		}
#endif
		(this as IPluginComponent).SetEnabledState(Config.Enabled);
	}

	#endregion

	protected override void OnDispose()
	{
		Config.ValueChangeEvent -= OnConfigPropertyChanged;
		Config.Items.ForEach(x => x.ValueChangeEvent -= OnConfigPropertyChanged);
		Singletons.Get<ConfigurationManager>().Reset -= OnConfigReset;

		_items.ForEach(item => item.Dispose());
		_items.Clear();
	}
}

#region Draggable Element

public class PluginMenuDraggableHudElement : DraggableHudElement
{
	private readonly PluginMenu _parent;

	public PluginMenuDraggableHudElement(PluginMenu parent, string? displayName = null, string? id = null) : base((AnchorablePluginConfigObject) parent.GetConfig(), displayName, id)
	{
		_parent = parent;
	}

	protected override Vector2 GetSize() => _parent.Size.X != 0 && _parent.Size.Y != 0 ? _parent.Size : _parent.SizePreview;

	protected override void SetSize(Vector2 value)
	{
		// Size is calculated by child elements
	}
}

#endregion