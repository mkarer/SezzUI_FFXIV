using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.Command;
using Dalamud.Plugin;
using FFXIVClientStructs;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiScene;
using SezzUI.Configuration;
using SezzUI.Configuration.Profiles;
using SezzUI.Enums;
using SezzUI.Game.Events;
using SezzUI.Helper;
using SezzUI.Hooking;
using SezzUI.Interface;
using SezzUI.Interface.GeneralElements;
using SezzUI.Logging;
using SezzUI.Modules.Test;

namespace SezzUI
{
	public class Plugin : IDalamudPlugin
	{
		public string Name => "SezzUI";
		public static string Version { get; private set; } = "";
		public static string AssemblyLocation { get; private set; } = "";
#if DEBUG
		public static GeneralDebugConfig DebugConfig { get; private set; } = null!;
#endif
		public static PluginLogger Logger = new();
		public static TextureWrap? BannerTexture;

		public static readonly NumberFormatInfo NumberFormatInfo = CultureInfo.GetCultureInfo("en-GB").NumberFormat;

		private readonly ConfigurationManager _configurationManager;
		private readonly HudManager _hudManager;

		public Plugin(DalamudPluginInterface pluginInterface)
		{
			pluginInterface.Create<Service>();
			Resolver.Initialize(Service.SigScanner.SearchBase);

			AssemblyLocation = pluginInterface.AssemblyLocation.DirectoryName != null ? pluginInterface.AssemblyLocation.DirectoryName : Assembly.GetExecutingAssembly().Location;
			AssemblyLocation = AssemblyLocation.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
			Version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0.12";

			LoadBanner();

			// initialize a not-necessarily-defaults configuration
			_configurationManager = new();
			Singletons.Register(_configurationManager, 100);
			ProfilesManager.Initialize();
			_configurationManager.LoadOrInitializeFiles();
#if DEBUG
			DebugConfig = _configurationManager.GetConfigObject<GeneralDebugConfig>();
			_configurationManager.ResetEvent += OnConfigReset;
#endif

			Singletons.Register(new MediaManager(), 70);
			Singletons.Register(new ClipRectsHelper(), 50);
			Singletons.Register(new GlobalColors(), 50);
			Singletons.Register(new TexturesCache(), 80);
			Singletons.Register(new ImageCache(), 80);
			Singletons.Register(new TooltipsHelper(), 80);
			Singletons.Register(new OriginalFunctionManager(), 60);
			Singletons.Register(new EventManager(), 40);

			_hudManager = new();
			Singletons.Register(_hudManager, 40);

			Service.PluginInterface.UiBuilder.DisableAutomaticUiHide = true;
			Service.PluginInterface.UiBuilder.DisableCutsceneUiHide = true;
			Service.PluginInterface.UiBuilder.DisableGposeUiHide = true;
			Service.PluginInterface.UiBuilder.DisableUserUiHide = true;
			Service.PluginInterface.UiBuilder.Draw += Draw;
			Service.PluginInterface.UiBuilder.OpenConfigUi += OpenConfigUi;

			Service.CommandManager.AddHandler("/sezzui", new(PluginCommand)
			{
				HelpMessage = "Opens the SezzUI configuration window.",
				ShowInHelp = true
			});
			CommandInfo alias = new(PluginCommand) {ShowInHelp = false};
			Service.CommandManager.AddHandler("/sezz", alias);

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

		private static void LoadBanner()
		{
			string bannerImage = Path.Combine(Path.GetDirectoryName(AssemblyLocation) ?? "", "Media", "Images", "banner_short_x150.png");

			if (File.Exists(bannerImage))
			{
				try
				{
					BannerTexture = Service.PluginInterface.UiBuilder.LoadImage(bannerImage);
				}
				catch (Exception ex)
				{
					Logger.Error($"Error loading banner from file ({bannerImage}): {ex}");
				}
			}
			else
			{
				Logger.Error($"Banner image doesn't exist: {bannerImage}");
			}
		}

		private static void PluginCommand(string command, string arguments)
		{
			ConfigurationManager configurationManager = Singletons.Get<ConfigurationManager>();

			if (configurationManager.IsConfigWindowOpened && !configurationManager.LockHUD)
			{
				configurationManager.LockHUD = true;
			}
			else
			{
				switch (arguments)
				{
					case "toggle":
						configurationManager.ShowHUD = !configurationManager.ShowHUD;
						break;

					case "show":
						configurationManager.ShowHUD = true;
						break;

					case "hide":
						configurationManager.ShowHUD = false;
						break;

#if DEBUG
					case "test":
						Test.RunTest();
						break;
#endif

					case { } argument when argument.StartsWith("profile"):
						string[] profile = argument.Split(" ", 2);
						if (profile.Length > 0)
						{
							ProfilesManager.Instance.CheckUpdateSwitchCurrentProfile(profile[1]);
						}
						break;

					default:
						configurationManager.ToggleConfigWindow();
						break;
				}
			}
		}

		private void Draw()
		{
			Service.PluginInterface.UiBuilder.OverrideGameCursor = false;

			DrawState drawState = GetDrawState();
			if (DrawState != drawState)
			{
				DrawState = drawState;
#if DEBUG
				if (DebugConfig.LogEvents && DebugConfig.LogEventPluginDrawStateChanged)
				{
					Logger.Debug($"DrawStateChanged: {drawState}");
					DrawStateChanged?.Invoke(drawState);
				}
#endif
			}

			_configurationManager.Draw();
			using (MediaManager.PushFont())
			{
				_hudManager.Draw(drawState);
			}
		}

		#region Draw State

		public delegate void DrawStateChangedDelegate(DrawState drawState);

#pragma warning disable CS0067
		public static event DrawStateChangedDelegate? DrawStateChanged;
#pragma warning restore CS0067
		public static DrawState DrawState { get; private set; } = DrawState.Unknown;

		private static unsafe bool IsAddonVisible(string name)
		{
			try
			{
				IntPtr addon = Service.GameGui.GetAddonByName(name, 1);
				return addon != IntPtr.Zero && ((AtkUnitBase*) addon)->IsVisible;
			}
			catch (Exception ex)
			{
				Logger.Error(ex);
			}

			return false;
		}

		private static DrawState GetDrawState()
		{
			// TODO: Flags for partial state (_NaviMap,  _ActionBar, others?)
			// Dalamud conditions
			if (!Service.ClientState.IsLoggedIn || Service.Condition[ConditionFlag.CreatingCharacter] || Service.Condition[ConditionFlag.BetweenAreas] || Service.Condition[ConditionFlag.BetweenAreas51] || Service.ClientState.LocalPlayer == null)
			{
				return DrawState.HiddenNotInGame;
			}

			if (!Singletons.Get<ConfigurationManager>().ShowHUD || Service.GameGui.GameUiHidden)
			{
				return DrawState.HiddenDisabled;
			}

			if (Service.Condition[ConditionFlag.WatchingCutscene] || Service.Condition[ConditionFlag.WatchingCutscene78] || Service.Condition[ConditionFlag.OccupiedInCutSceneEvent])
			{
				// WatchingCutscene includes Group Pose
				return IsAddonVisible("_NaviMap") ? DrawState.Partially : DrawState.HiddenCutscene;
			}

			if (Service.Condition[ConditionFlag.OccupiedSummoningBell])
			{
				return DrawState.Partially;
			}

			// Quest interaction
			if (Service.Condition[ConditionFlag.OccupiedInQuestEvent] || Service.Condition[ConditionFlag.OccupiedInEvent])
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
			Singletons.Get<ConfigurationManager>().ToggleConfigWindow();
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!disposing)
			{
				return;
			}

			Service.PluginInterface.UiBuilder.Draw -= Draw;
			Service.PluginInterface.UiBuilder.OpenConfigUi -= OpenConfigUi;
			Service.PluginInterface.UiBuilder.RebuildFonts();

			Service.CommandManager.RemoveHandler("/sezzui");
			Service.CommandManager.RemoveHandler("/sezz");

#if DEBUG
			_configurationManager.ResetEvent -= OnConfigReset;
#endif
			_configurationManager.SaveConfigurations(true);
			_configurationManager.CloseConfigWindow();

			ProfilesManager.Instance.Dispose();
			Singletons.Dispose();
		}
	}
}