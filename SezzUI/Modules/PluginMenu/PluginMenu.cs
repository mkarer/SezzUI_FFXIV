using System;
using System.Linq;
using System.Numerics;
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
		private XivCommonBase xivCommon;
		
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
			
			int enabledButtons = Config.Items.Count(item => item.Enabled);
			if (enabledButtons == 0)
			{
				return;
			}

			IntPtr nowLoading = Plugin.GameGui.GetAddonByName("NowLoading", 1);
			AtkResNode* nowLoadingNode = ((AtkUnitBase*) nowLoading)->RootNode;
			float opacity = !nowLoadingNode->IsVisible ? 1f : Math.Min(160f, 160 - nowLoadingNode->Alpha_2) / 160f; // At about 172 the _NaviMap is hidden here.

			Vector2 buttonSize = new(60f, 30f);
			uint buttonPadding = 4;
			uint borderSize = 1;
			bool rightToLeft = Config.Anchor is DrawAnchor.Right or DrawAnchor.TopRight or DrawAnchor.BottomRight;

			// TODO: Calculate real size upon OnConfigPropertyChanged/OnEnable
			Vector2 menuSize = new(enabledButtons * buttonSize.X + (enabledButtons - 1) * buttonPadding + borderSize, buttonSize.Y + 2);
			menuSize.X += rightToLeft ? borderSize : 0; // Needed for R2L?
			Vector2 menuPos = DrawHelper.GetAnchoredPosition(menuSize, Config.Anchor);
			menuPos.X += Config.Position.X;
			menuPos.Y += Config.Position.Y;

			// Buttons
			ImGui.SetNextWindowSize(menuSize, ImGuiCond.Always);
			ImGui.SetNextWindowContentSize(menuSize);
			ImGui.SetNextWindowPos(menuPos);

			ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(borderSize, borderSize)); // Would clip some borders otherwise...
			ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 0);
			ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize, borderSize);

			ImGui.PushStyleColor(ImGuiCol.Button, ImGui.ColorConvertFloat4ToU32(new(0f, 0f, 0f, 0.5f * opacity)));
			ImGui.PushStyleColor(ImGuiCol.ButtonHovered, ImGui.ColorConvertFloat4ToU32(new(1f, 1f, 1f, 0.15f * opacity)));
			ImGui.PushStyleColor(ImGuiCol.ButtonActive, ImGui.ColorConvertFloat4ToU32(new(1f, 1f, 1f, 0.25f * opacity)));
			ImGui.PushStyleColor(ImGuiCol.Border, ImGui.ColorConvertFloat4ToU32(new(1f, 1f, 1f, 77f / 255f * opacity)));

			float buttonOffset = rightToLeft ? menuSize.X - buttonSize.X - borderSize : 0f;

			DelvUI.Helpers.DrawHelper.DrawInWindow("SezzUI_PluginMenu_Buttons", menuPos, menuSize, true, false, drawList =>
			{
				uint buttonId = 0;
				foreach (PluginMenuItemConfig item in Config.Items.Where(item => item.Enabled))
				{
					// ReSharper disable once AccessToModifiedClosure
					ImGui.SameLine(buttonOffset, 0f);

					if (ImGui.Button($"##SezzUI_PMB{buttonId}", buttonSize))
					{
						LogDebug($"Menu item clicked: {nameof(item)}");
						if (item.Command.StartsWith("/"))
						{
							LogDebug($"Executing command: {item.Command}");
							xivCommon.Functions.Chat.SendMessage(item.Command);
						}
					}
					
					// ReSharper disable once AccessToModifiedClosure
					buttonOffset += rightToLeft ? -buttonSize.X - buttonPadding : buttonSize.X + buttonPadding;
					buttonId++;
				}
			});

			ImGui.PopStyleColor(4);
			ImGui.PopStyleVar(3);

			// Content
			ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(borderSize, borderSize)); // Would clip some borders otherwise...
			ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 0);
			buttonOffset = rightToLeft ? menuSize.X - buttonSize.X - borderSize : 0f;

			DelvUI.Helpers.DrawHelper.DrawInWindow("SezzUI_PluginMenu_Content", menuPos, menuSize, false, false, drawList =>
			{
				foreach (PluginMenuItemConfig item in Config.Items.Where(item => item.Enabled))
				{
					DrawHelper.DrawCenteredShadowText("MyriadProLightCond_16", item.Title, new(menuPos.X + buttonOffset, menuPos.Y), buttonSize / 2f, ImGui.ColorConvertFloat4ToU32(item.TextColor.Vector.AddTransparency(opacity)), ImGui.ColorConvertFloat4ToU32(new(0, 0, 0,  opacity)), drawList);
					buttonOffset += rightToLeft ? -((int) buttonSize.X + buttonPadding) : (int) buttonSize.X + buttonPadding;
				}
			});
			ImGui.PopStyleVar(2);
		}

		#region Constructor

		private PluginMenu(PluginConfigObject config) : base(config)
		{
#if DEBUG
			_debugConfig = ConfigurationManager.Instance.GetConfigObject<PluginMenuDebugConfig>();
#endif
			xivCommon = new();
			config.ValueChangeEvent += OnConfigPropertyChanged;
			ConfigurationManager.Instance.ResetEvent += OnConfigReset;
			Toggle(Config.Enabled);
		}

		#endregion

		#region Configuration Events

		private void OnConfigPropertyChanged(object sender, OnChangeBaseArgs args)
		{
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
			}

			_config = sender.GetConfigObject<PluginMenuConfig>();
			_config.ValueChangeEvent += OnConfigPropertyChanged;

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
			xivCommon.Dispose();
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