using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using Dalamud.Data;
using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Buddy;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.JobGauge;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using Dalamud.Interface;
using Dalamud.Plugin;
using DelvUI.Helpers;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;
using ImGuiScene;
using SezzUI.Config;
using SezzUI.Config.Profiles;
using SezzUI.Core;
using SezzUI.Enums;
using SezzUI.Helpers;
using SezzUI.Hooking;
using SezzUI.Interface;
using SezzUI.Interface.GeneralElements;

namespace SezzUI
{
	public class Plugin : IDalamudPlugin
	{
		#region Dalamud Services

		public static BuddyList BuddyList { get; private set; } = null!;
		public static ClientState ClientState { get; private set; } = null!;
		private static CommandManager CommandManager { get; set; } = null!;
		public static Condition Condition { get; private set; } = null!;
		public static DalamudPluginInterface PluginInterface { get; private set; } = null!;
		public static DataManager DataManager { get; private set; } = null!;
		public static Framework Framework { get; private set; } = null!;
		public static GameGui GameGui { get; private set; } = null!;
		public static JobGauges JobGauges { get; private set; } = null!;
		public static ObjectTable ObjectTable { get; private set; } = null!;
		public static SigScanner SigScanner { get; private set; } = null!;
		public static TargetManager TargetManager { get; private set; } = null!;
		public static UiBuilder UiBuilder { get; private set; } = null!;
		public static ChatGui ChatGui { get; private set; } = null!;

		#endregion

		public static PluginLogger Logger = new();
		public static TextureWrap? BannerTexture;
		public static string AssemblyLocation { get; private set; } = "";
		public string Name => "SezzUI";
		public static string Version { get; private set; } = "";
#if DEBUG
		public static GeneralDebugConfig DebugConfig { get; private set; } = null!;
#endif

		public static readonly NumberFormatInfo NumberFormatInfo = CultureInfo.GetCultureInfo("en-GB").NumberFormat;

		public Plugin(BuddyList buddyList, ClientState clientState, CommandManager commandManager, Condition condition, DalamudPluginInterface pluginInterface, DataManager dataManager, Framework framework, GameGui gameGui, JobGauges jobGauges, ObjectTable objectTable, SigScanner sigScanner, TargetManager targetManager, ChatGui chatGui)
		{
			BuddyList = buddyList;
			ClientState = clientState;
			CommandManager = commandManager;
			Condition = condition;
			PluginInterface = pluginInterface;
			DataManager = dataManager;
			Framework = framework;
			GameGui = gameGui;
			JobGauges = jobGauges;
			ObjectTable = objectTable;
			SigScanner = sigScanner;
			TargetManager = targetManager;
			ChatGui = chatGui;
			UiBuilder = PluginInterface.UiBuilder;

			if (pluginInterface.AssemblyLocation.DirectoryName != null)
			{
				AssemblyLocation = pluginInterface.AssemblyLocation.DirectoryName + "\\";
			}
			else
			{
				AssemblyLocation = Assembly.GetExecutingAssembly().Location;
			}

			Version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0.7";

			FontsManager.Initialize(AssemblyLocation);
			LoadBanner();

			// initialize a not-necessarily-defaults configuration
			ConfigurationManager.Initialize();
			ProfilesManager.Initialize();
			ConfigurationManager.Instance.LoadOrInitializeFiles();
#if DEBUG
			DebugConfig = ConfigurationManager.Instance.GetConfigObject<GeneralDebugConfig>();
			ConfigurationManager.Instance.ResetEvent += OnConfigReset;
#endif

			FontsManager.Instance.LoadConfig();

			ClipRectsHelper.Initialize();
			GlobalColors.Initialize();
			TexturesCache.Initialize();
			ImageCache.Initialize();
			TooltipsHelper.Initialize();
			OriginalFunctionManager.Initialize();
			EventManager.Initialize();
			HudManager.Initialize();

			UiBuilder.DisableAutomaticUiHide = true;
			UiBuilder.DisableCutsceneUiHide = true;
			UiBuilder.DisableGposeUiHide = true;
			UiBuilder.DisableUserUiHide = true;

			UiBuilder.Draw += Draw;
			UiBuilder.BuildFonts += BuildFont;
			UiBuilder.OpenConfigUi += OpenConfigUi;

			CommandManager.AddHandler("/sezzui", new(PluginCommand)
			{
				HelpMessage = "Opens the SezzUI configuration window.",
				ShowInHelp = true
			});
			CommandInfo alias = new(PluginCommand) {ShowInHelp = false};
			CommandManager.AddHandler("/sezz", alias);
			CommandManager.AddHandler("/sui", alias);

#if DEBUG
			if (DebugConfig?.ShowConfigurationOnLogin ?? false)
			{
				OpenConfigUi();
			}
#endif
		}

#if DEBUG
		private static void OnConfigReset(ConfigurationManager sender)
		{
			DebugConfig = sender.GetConfigObject<GeneralDebugConfig>();
		}
#endif

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		private static void BuildFont()
		{
			FontsManager.Instance.BuildFonts();
		}

		private static void LoadBanner()
		{
			string bannerImage = Path.Combine(Path.GetDirectoryName(AssemblyLocation) ?? "", "Media", "Images", "banner_short_x150.png");

			if (File.Exists(bannerImage))
			{
				try
				{
					BannerTexture = UiBuilder.LoadImage(bannerImage);
				}
				catch (Exception ex)
				{
					Logger.Error(ex, "LoadBanner", "Error loading banner from file ({bannerImage}): {ex}");
				}
			}
			else
			{
				Logger.Error("LoadBanner", $"Banner image doesn't exist. {bannerImage}");
			}
		}

		private static void PluginCommand(string command, string arguments)
		{
			ConfigurationManager configManager = ConfigurationManager.Instance;

			if (configManager.IsConfigWindowOpened && !configManager.LockHUD)
			{
				configManager.LockHUD = true;
			}
			else
			{
				switch (arguments)
				{
					case "toggle":
						ConfigurationManager.Instance.ShowHUD = !ConfigurationManager.Instance.ShowHUD;
						break;

					case "show":
						ConfigurationManager.Instance.ShowHUD = true;
						break;

					case "hide":
						ConfigurationManager.Instance.ShowHUD = false;
						break;

					case "test":
						Test.RunTest();
						break;

					case { } argument when argument.StartsWith("profile"):
						string[] profile = argument.Split(" ", 2);
						if (profile.Length > 0)
						{
							ProfilesManager.Instance.CheckUpdateSwitchCurrentProfile(profile[1]);
						}

						break;

					default:
						configManager.ToggleConfigWindow();
						break;
				}
			}
		}

		public delegate void DrawStateChangedDelegate(DrawState drawState);

		public static event DrawStateChangedDelegate? DrawStateChanged;

		private void Draw()
		{
			UiBuilder.OverrideGameCursor = false;
			ConfigurationManager.Instance.Draw();

			// ReSharper disable once ConditionIsAlwaysTrueOrFalse
			if (HudManager.Instance != null)
			{
				bool fontPushed = FontsManager.Instance.PushDefaultFont();

				DrawState drawState = GetDrawState();
				if (DrawState != drawState)
				{
					DrawState = drawState;
#if DEBUG
					if (DebugConfig.LogEvents && DebugConfig.LogEventPluginDrawStateChanged)
					{
						Logger.Debug("Draw", $"DrawStateChanged: {drawState}");
						DrawStateChanged?.Invoke(drawState);
					}
#endif
				}

				HudManager.Instance.Draw(drawState);

				if (fontPushed)
				{
					ImGui.PopFont();
				}
			}
#if DEBUG
			else if (DebugConfig.LogEvents && DebugConfig.LogEventPluginDrawStateChanged)
			{
				Logger.Debug("Draw", "HudManager is NULL!");
			}
#endif
		}

		#region Draw State

		public static DrawState DrawState { get; private set; } = DrawState.Unknown;

		private static unsafe bool IsAddonVisible(string name)
		{
			try
			{
				IntPtr addon = GameGui.GetAddonByName(name, 1);
				return addon != IntPtr.Zero && ((AtkUnitBase*) addon)->IsVisible;
			}
			catch (Exception ex)
			{
				Logger.Error(ex, $"[IsAddonVisible] Error: {ex}");
			}

			return false;
		}

		private static DrawState GetDrawState()
		{
			// TODO: Flags for partial state (_NaviMap,  _ActionBar, others?)
			// Dalamud conditions
			if (!ClientState.IsLoggedIn || Condition[ConditionFlag.CreatingCharacter] || Condition[ConditionFlag.BetweenAreas] || Condition[ConditionFlag.BetweenAreas51] || ClientState.LocalPlayer == null)
			{
				return DrawState.HiddenNotInGame;
			}

			if (!ConfigurationManager.Instance.ShowHUD || GameGui.GameUiHidden)
			{
				return DrawState.HiddenDisabled;
			}

			if (Condition[ConditionFlag.WatchingCutscene] || Condition[ConditionFlag.WatchingCutscene78] || Condition[ConditionFlag.OccupiedInCutSceneEvent])
			{
				// WatchingCutscene includes Group Pose
				return IsAddonVisible("_NaviMap") ? DrawState.Partially : DrawState.HiddenCutscene;
			}

			if (Condition[ConditionFlag.OccupiedSummoningBell])
			{
				return DrawState.Partially;
			}

			// Quest interaction
			if (Condition[ConditionFlag.OccupiedInQuestEvent] || Condition[ConditionFlag.OccupiedInEvent])
			{
				return DrawState.Partially;
			}

			if (!IsAddonVisible("_ParameterWidget") || IsAddonVisible("FadeMiddle") || !IsAddonVisible("_ActionBar"))
			{
				return DrawState.Partially;
			}

			return DrawState.Visible;
		}

		#endregion

		private static void OpenConfigUi()
		{
			ConfigurationManager.Instance.ToggleConfigWindow();
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!disposing)
			{
				return;
			}

			HudManager.Instance.Dispose();
			EventManager.Instance.Dispose();

#if DEBUG
			ConfigurationManager.Instance.ResetEvent -= OnConfigReset;
#endif
			ConfigurationManager.Instance.SaveConfigurations(true);
			ConfigurationManager.Instance.CloseConfigWindow();

			CommandManager.RemoveHandler("/sezzui");
			CommandManager.RemoveHandler("/sezz");
			CommandManager.RemoveHandler("/sui");

			UiBuilder.Draw -= Draw;
			UiBuilder.BuildFonts -= BuildFont;
			UiBuilder.OpenConfigUi -= OpenConfigUi;
			UiBuilder.RebuildFonts();

			ClipRectsHelper.Instance.Dispose();
			FontsManager.Instance.Dispose();
			GlobalColors.Instance.Dispose();
			ProfilesManager.Instance.Dispose();
			TexturesCache.Instance.Dispose();
			ImageCache.Instance.Dispose();
			TooltipsHelper.Instance.Dispose();
			OriginalFunctionManager.Instance.Dispose();

			// This needs to remain last to avoid race conditions
			ConfigurationManager.Instance.Dispose();

			Logger.Debug("Goodbye!");
		}
	}
}