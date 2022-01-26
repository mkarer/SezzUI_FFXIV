using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;
using SezzUI.Config;
using SezzUI.Enums;
using SezzUI.Helpers;
using SezzUI.Interface.GeneralElements;
using XivCommon;

namespace SezzUI.Modules.PluginMenu
{
	public class PluginMenu : HudModule
	{
		private PluginMenuConfig Config => (PluginMenuConfig) _config;
#if DEBUG
		private PluginMenuDebugConfig _debugConfig;
#endif
		private XivCommonBase _xivCommon;
		private List<PluginMenuItem> _items;
		private static long _lastError = 0;
		private const byte BORDER_SIZE = 1;
		private const byte BUTTON_PADDING = 4;

		public override unsafe void Draw(DrawState drawState, Vector2? origin)
		{
			if (!Enabled || drawState != DrawState.Visible && drawState != DrawState.Partially)
			{
				return;
			}

			// IntPtr naviMap = Plugin.GameGui.GetAddonByName("_NaviMap", 1);
			// if (naviMap == IntPtr.Zero || !((AtkUnitBase*) naviMap)->IsVisible)
			// {
			// 	return;
			// }

			List<PluginMenuItem> enabledItems = _items.Where(item => item.Config.Enabled).ToList();
			if (enabledItems.Count() == 0)
			{
				return;
			}

			IntPtr nowLoading = Plugin.GameGui.GetAddonByName("NowLoading", 1);
			AtkResNode* nowLoadingNode = ((AtkUnitBase*) nowLoading)->RootNode;
			float opacity = !nowLoadingNode->IsVisible ? 1f : Math.Min(160f, 160 - nowLoadingNode->Alpha_2) / 160f; // At about 172 the _NaviMap is hidden here.
			if (opacity <= 0)
			{
				return;
			}

			bool rightToLeft = Config.Anchor is DrawAnchor.Right or DrawAnchor.TopRight or DrawAnchor.BottomRight;

			Vector2 menuSize = new(enabledItems.Sum(item => item.Size.X) + 2 * BORDER_SIZE + (enabledItems.Count() - 1) * BUTTON_PADDING, enabledItems[0].Size.Y + 2 * BORDER_SIZE);
			Vector2 menuPos = DrawHelper.GetAnchoredPosition(menuSize, Config.Anchor);
			menuPos.X += Config.Position.X;
			menuPos.Y += Config.Position.Y;

			// Buttons
			ImGui.SetNextWindowSize(menuSize, ImGuiCond.Always);
			ImGui.SetNextWindowContentSize(menuSize);
			ImGui.SetNextWindowPos(menuPos);

			ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(BORDER_SIZE, BORDER_SIZE)); // Would clip some borders otherwise...
			ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 0);
			ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize, BORDER_SIZE);

			ImGui.PushStyleColor(ImGuiCol.Button, ImGui.ColorConvertFloat4ToU32(new(0f, 0f, 0f, 0.5f * opacity)));
			ImGui.PushStyleColor(ImGuiCol.ButtonHovered, ImGui.ColorConvertFloat4ToU32(new(1f, 1f, 1f, 0.15f * opacity)));
			ImGui.PushStyleColor(ImGuiCol.ButtonActive, ImGui.ColorConvertFloat4ToU32(new(1f, 1f, 1f, 0.25f * opacity)));
			ImGui.PushStyleColor(ImGuiCol.Border, ImGui.ColorConvertFloat4ToU32(new(1f, 1f, 1f, 77f / 255f * opacity)));

			DelvUI.Helpers.DrawHelper.DrawInWindow("SezzUI_PluginMenu_Buttons", menuPos, menuSize, true, false, drawList =>
			{
				float buttonOffset = rightToLeft ? menuSize.X - enabledItems[0].Size.X - BORDER_SIZE : BORDER_SIZE; // Need to add border size here for R2L or it will clip the right border. 
				uint shadowColor = ImGui.ColorConvertFloat4ToU32(new(0, 0, 0, opacity));

				for (int i = 0; i < enabledItems.Count(); i++)
				{
					PluginMenuItem item = enabledItems[i];
					ImGui.SameLine(buttonOffset, 0f);

					// Button
					if (ImGui.Button($"##SezzUI_PMB{i}", item.Size))
					{
						LogDebug($"Menu item clicked: {nameof(item)}");
						item.Toggle();
						if (item.Config.Command.StartsWith("/"))
						{
							LogDebug($"Executing command: {item.Config.Command}");
							_xivCommon.Functions.Chat.SendMessage(item.Config.Command);
						}
					}
					
					// Text
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
						textPosition.Y += 1;

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
									DelvUI.Helpers.DrawHelper.DrawShadowText(match.Groups[2].Value, textPosition, ImGui.ColorConvertFloat4ToU32(color), shadowColor, drawList);
									textPosition.X += ImGui.CalcTextSize(match.Groups[2].Value).X;
								}
							}
						}
						catch (Exception ex)
						{
							long now = System.Environment.TickCount64;
							if (now - _lastError > 5000)
							{
								_lastError = now;
								LogError(ex, "Draw", $"Error: {ex}");
							}
						}
					}
					else
					{
						DrawHelper.DrawCenteredShadowText("MyriadProLightCond_16", item.Config.Title, buttonPos, item.Size / 2f, ImGui.ColorConvertFloat4ToU32(color.AddTransparency(opacity)), shadowColor, drawList);
					}
					
					if (i + 1 < enabledItems.Count())
					{
						buttonOffset += rightToLeft ? -enabledItems[i + 1].Size.X - BUTTON_PADDING : item.Size.X + BUTTON_PADDING;
					}
				}
			});

			ImGui.PopStyleColor(4);
			ImGui.PopStyleVar(3);
		}
		
		protected override bool Enable()
		{
			if (!base.Enable())
			{
				return false;
			}

			_items.ForEach(item => item.Update());
			return true;
		}

		#region Constructor

		private PluginMenu(PluginConfigObject config) : base(config)
		{
#if DEBUG
			_debugConfig = ConfigurationManager.Instance.GetConfigObject<PluginMenuDebugConfig>();
#endif
			_xivCommon = new();
			config.ValueChangeEvent += OnConfigPropertyChanged;
			Config.Item1.ValueChangeEvent += OnConfigPropertyChanged;
			Config.Item2.ValueChangeEvent += OnConfigPropertyChanged;
			Config.Item3.ValueChangeEvent += OnConfigPropertyChanged;
			Config.Item4.ValueChangeEvent += OnConfigPropertyChanged;
			Config.Item5.ValueChangeEvent += OnConfigPropertyChanged;
			Config.Item6.ValueChangeEvent += OnConfigPropertyChanged;
			Config.Item7.ValueChangeEvent += OnConfigPropertyChanged;
			Config.Item8.ValueChangeEvent += OnConfigPropertyChanged;
			Config.Item9.ValueChangeEvent += OnConfigPropertyChanged;
			Config.Item10.ValueChangeEvent += OnConfigPropertyChanged;
			
			ConfigurationManager.Instance.ResetEvent += OnConfigReset;
			_items = new() {new(Config.Item1), new(Config.Item2), new(Config.Item3), new(Config.Item4), new(Config.Item5), new(Config.Item6), new(Config.Item7), new(Config.Item8), new(Config.Item9), new(Config.Item10)};
			Toggle(Config.Enabled);
		}

		#endregion

		#region Configuration Events

		private void OnConfigPropertyChanged(object sender, OnChangeBaseArgs args)
		{
			_lastError = 0;

			if (sender is PluginMenuItemConfig itemConfig)
			{
				if (Config.Enabled)
				{
					_items.Where(item => item.Config == itemConfig).FirstOrDefault()?.Update();
				}
				return;
			}
			
			switch (args.PropertyName)
			{
				case "Enabled":
#if DEBUG
					if (_debugConfig.LogConfigurationManager)
					{
						LogDebug("OnConfigPropertyChanged", $"{args.PropertyName}: {Config.Enabled}");
					}
#endif
					Toggle(Config.Enabled);
					break;
			}
		}

		private void OnConfigReset(ConfigurationManager sender)
		{
			_lastError = 0;
#if DEBUG
			if (_debugConfig.LogConfigurationManager)
			{
				LogDebug("OnConfigReset", "Resetting...");
			}
#endif
			Disable();
			if (_config != null)
			{
				_config.ValueChangeEvent -= OnConfigPropertyChanged;
				Config.Item1.ValueChangeEvent -= OnConfigPropertyChanged;
				Config.Item2.ValueChangeEvent -= OnConfigPropertyChanged;
				Config.Item3.ValueChangeEvent -= OnConfigPropertyChanged;
				Config.Item4.ValueChangeEvent -= OnConfigPropertyChanged;
				Config.Item5.ValueChangeEvent -= OnConfigPropertyChanged;
				Config.Item6.ValueChangeEvent -= OnConfigPropertyChanged;
				Config.Item7.ValueChangeEvent -= OnConfigPropertyChanged;
				Config.Item8.ValueChangeEvent -= OnConfigPropertyChanged;
				Config.Item9.ValueChangeEvent -= OnConfigPropertyChanged;
				Config.Item10.ValueChangeEvent -= OnConfigPropertyChanged;
			}

			_config = sender.GetConfigObject<PluginMenuConfig>();
			_config.ValueChangeEvent += OnConfigPropertyChanged;
			Config.Item1.ValueChangeEvent += OnConfigPropertyChanged;
			Config.Item2.ValueChangeEvent += OnConfigPropertyChanged;
			Config.Item3.ValueChangeEvent += OnConfigPropertyChanged;
			Config.Item4.ValueChangeEvent += OnConfigPropertyChanged;
			Config.Item5.ValueChangeEvent += OnConfigPropertyChanged;
			Config.Item6.ValueChangeEvent += OnConfigPropertyChanged;
			Config.Item7.ValueChangeEvent += OnConfigPropertyChanged;
			Config.Item8.ValueChangeEvent += OnConfigPropertyChanged;
			Config.Item9.ValueChangeEvent += OnConfigPropertyChanged;
			Config.Item10.ValueChangeEvent += OnConfigPropertyChanged;

#if DEBUG
			_debugConfig = sender.GetConfigObject<PluginMenuDebugConfig>();
			if (_debugConfig.LogConfigurationManager)
			{
				LogDebug("OnConfigReset", $"Config.Enabled: {Config.Enabled}");
			}
#endif
			Toggle(Config.Enabled);
		}

		#endregion

		#region Finalizer

		protected override void InternalDispose()
		{
			Disable();
			ConfigurationManager.Instance.ResetEvent -= OnConfigReset;
			_items.ForEach(item => item.Dispose());
			_items.Clear();
			_xivCommon.Dispose();
		}

		#endregion

		#region Singleton

		public static void Initialize()
		{
			Instance = new(ConfigurationManager.Instance.GetConfigObject<PluginMenuConfig>());
		}

		public static PluginMenu Instance { get; private set; } = null!;

		~PluginMenu()
		{
			Dispose(false);
		}

		#endregion
	}
}