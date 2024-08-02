using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.Command;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Component.GUI;
using SezzUI.Configuration;
using SezzUI.Configuration.Profiles;
using SezzUI.Enums;
using SezzUI.Game.Events;
using SezzUI.Helper;
using SezzUI.Hooking;
using SezzUI.Interface;
using SezzUI.Interface.GeneralElements;
using SezzUI.Logging;
#if DEBUG
using SezzUI.Modules.Test;
#endif

namespace SezzUI;

public class Plugin : IDalamudPlugin
{
	public string Name => "SezzUI";
	public static string Version { get; private set; } = "";
	public static string AssemblyLocation { get; private set; } = "";
#if DEBUG
	public static GeneralDebugConfig DebugConfig { get; private set; } = null!;
#endif
	public static PluginLogger Logger = new();
	public static ISharedImmediateTexture? BannerTexture;

	public static readonly NumberFormatInfo NumberFormatInfo = CultureInfo.GetCultureInfo("en-GB").NumberFormat;

	private readonly ConfigurationManager _configurationManager;
	private readonly HudManager _hudManager;

	public Plugin(IDalamudPluginInterface pluginInterface)
	{
		Services? services = pluginInterface.Create<Services>();
		if (services == null)
		{
			throw new("Could not create services!");
		}

		AssemblyLocation = pluginInterface.AssemblyLocation.DirectoryName != null ? pluginInterface.AssemblyLocation.DirectoryName : Assembly.GetExecutingAssembly().Location;
		AssemblyLocation = AssemblyLocation.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
		Version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.7.0";

#if DEBUG
		Logger.Debug($"{Name} Version {Version}");
#endif

		// initialize a not-necessarily-defaults configuration
		_configurationManager = new();
		Singletons.Register(_configurationManager, 100);
		ProfilesManager.Initialize();
		_configurationManager.LoadOrInitializeFiles();
#if DEBUG
		DebugConfig = _configurationManager.GetConfigObject<GeneralDebugConfig>();
		_configurationManager.ResetEvent += OnConfigReset;
#endif

		Singletons.Register(new MediaManager(pluginInterface.UiBuilder), 70);
		Singletons.Register(new ClipRectsHelper(), 50);
		Singletons.Register(new GlobalColors(), 50);
		Singletons.Register(new TooltipsHelper(), 80);
		Singletons.Register(new OriginalFunctionManager(), 60);
		Singletons.Register(new EventManager(), 40);

		_hudManager = new();
		Singletons.Register(_hudManager, 40);

		Services.PluginInterface.UiBuilder.DisableAutomaticUiHide = true;
		Services.PluginInterface.UiBuilder.DisableCutsceneUiHide = true;
		Services.PluginInterface.UiBuilder.DisableGposeUiHide = true;
		Services.PluginInterface.UiBuilder.DisableUserUiHide = true;
		Services.PluginInterface.UiBuilder.Draw += Draw;
		Services.PluginInterface.UiBuilder.OpenConfigUi += OpenConfigUi;
		Services.PluginInterface.UiBuilder.OpenMainUi += OpenConfigUi;

		Services.Commands.AddHandler("/sezzui", new(PluginCommand)
		{
			HelpMessage = "Opens the SezzUI configuration window.",
			ShowInHelp = true
		});
		CommandInfo alias = new(PluginCommand) {ShowInHelp = false};
		Services.Commands.AddHandler("/sezz", alias);

#if DEBUG
		if (DebugConfig.ShowConfigurationOnLogin)
		{
			Services.Framework.Update += OpenConfigUiOnFrameworkUpdate;
		}
#endif
	}

#if DEBUG
	private static void OnConfigReset(ConfigurationManager sender)
	{
		DebugConfig = sender.GetConfigObject<GeneralDebugConfig>();
	}

	private void OpenConfigUiOnFrameworkUpdate(IFramework unused)
	{
		Services.Framework.Update -= OpenConfigUiOnFrameworkUpdate;
		OpenConfigUi();
	}

#endif

	public static IDalamudTextureWrap? GetBanner()
	{
		if (BannerTexture == null && ThreadSafety.IsMainThread)
		{
			string bannerImage = Path.Combine(Path.GetDirectoryName(AssemblyLocation) ?? "", "Media", "Images", "Banner150.png");
			try
			{
				BannerTexture = Services.TextureProvider.GetFromFile(bannerImage);
			}
			catch (Exception ex)
			{
				Logger.Error($"Error loading banner from file ({bannerImage}): {ex}");
			}
		}

		return BannerTexture?.GetWrapOrEmpty();
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
		Services.PluginInterface.UiBuilder.OverrideGameCursor = false;

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
			IntPtr addon = Services.GameGui.GetAddonByName(name);
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
		if (!Services.ClientState.IsLoggedIn || Services.Condition[ConditionFlag.CreatingCharacter] || Services.Condition[ConditionFlag.BetweenAreas] || Services.Condition[ConditionFlag.BetweenAreas51] || Services.ClientState.LocalPlayer == null)
		{
			return DrawState.HiddenNotInGame;
		}

		if (!Singletons.Get<ConfigurationManager>().ShowHUD || Services.GameGui.GameUiHidden)
		{
			return DrawState.HiddenDisabled;
		}

		if (Services.Condition[ConditionFlag.WatchingCutscene] || Services.Condition[ConditionFlag.WatchingCutscene78] || Services.Condition[ConditionFlag.OccupiedInCutSceneEvent])
		{
			// WatchingCutscene includes Group Pose
			return IsAddonVisible("_NaviMap") ? DrawState.Partially : DrawState.HiddenCutscene;
		}

		if (Services.Condition[ConditionFlag.OccupiedSummoningBell])
		{
			return DrawState.Partially;
		}

		// Quest interaction
		if (Services.Condition[ConditionFlag.OccupiedInQuestEvent] || Services.Condition[ConditionFlag.OccupiedInEvent])
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

		Services.PluginInterface.UiBuilder.Draw -= Draw;
		Services.PluginInterface.UiBuilder.OpenConfigUi -= OpenConfigUi;
		Services.PluginInterface.UiBuilder.OpenMainUi -= OpenConfigUi;
		Services.PluginInterface.UiBuilder.FontAtlas.BuildFontsAsync();
#if DEBUG
		Services.Framework.Update -= OpenConfigUiOnFrameworkUpdate;
#endif

		Services.Commands.RemoveHandler("/sezzui");
		Services.Commands.RemoveHandler("/sezz");

#if DEBUG
		_configurationManager.ResetEvent -= OnConfigReset;
#endif
		_configurationManager.SaveConfigurations(true);
		_configurationManager.CloseConfigWindow();

		ProfilesManager.Instance.Dispose();
		Singletons.Dispose();
	}
}