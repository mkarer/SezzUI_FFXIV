using System;
using System.IO;
using System.Reflection;
using System.Globalization;
using Dalamud.Data;
using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Buddy;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.JobGauge;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Party;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using Dalamud.Interface;
using Dalamud.Logging;
using Dalamud.Plugin;
using SezzUI.Config;
using SezzUI.Config.Profiles;
using SezzUI.Interface;
using SezzUI.Interface.GeneralElements;
using ImGuiNET;
using ImGuiScene;
using SigScanner = Dalamud.Game.SigScanner;

namespace SezzUI
{
    public class Plugin : IDalamudPlugin
    {
        public static BuddyList BuddyList { get; private set; } = null!;
        public static ClientState ClientState { get; private set; } = null!;
        public static CommandManager CommandManager { get; private set; } = null!;
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
        public static PartyList PartyList { get; private set; } = null!;

        public static TextureWrap? BannerTexture;

        public static string AssemblyLocation { get; private set; } = "";
        public string Name => "SezzUI";

        public static string Version { get; private set; } = "";

        private HudManager _hudManager = null!;
        public static NumberFormatInfo NumberFormatInfo = CultureInfo.GetCultureInfo("en-GB").NumberFormat;

        public Plugin(
            BuddyList buddyList,
            ClientState clientState,
            CommandManager commandManager,
            Condition condition,
            DalamudPluginInterface pluginInterface,
            DataManager dataManager,
            Framework framework,
            GameGui gameGui,
            JobGauges jobGauges,
            ObjectTable objectTable,
            PartyList partyList,
            SigScanner sigScanner,
            TargetManager targetManager
        )
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
            PartyList = partyList;
            SigScanner = sigScanner;
            TargetManager = targetManager;
            UiBuilder = PluginInterface.UiBuilder;

            if (pluginInterface.AssemblyLocation.DirectoryName != null)
            {
                AssemblyLocation = pluginInterface.AssemblyLocation.DirectoryName + "\\";
            }
            else
            {
                AssemblyLocation = Assembly.GetExecutingAssembly().Location;
            }

            Version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.6.1.1";

            DelvUI.Helpers.FontsManager.Initialize(AssemblyLocation);
            LoadBanner();

            // initialize a not-necessarily-defaults configuration
            ConfigurationManager.Initialize();
            ProfilesManager.Initialize();
            ConfigurationManager.Instance.LoadOrInitializeFiles();

            DelvUI.Helpers.FontsManager.Instance.LoadConfig();

            DelvUI.Helpers.ClipRectsHelper.Initialize();
            GlobalColors.Initialize();
            DelvUI.Helpers.TexturesCache.Initialize();
            Helpers.ImageCache.Initialize();
            DelvUI.Helpers.TooltipsHelper.Initialize();
            EventManager.Initialize();
            ModuleManager.Initialize();

            _hudManager = new HudManager();

            UiBuilder.Draw += Draw;
            UiBuilder.BuildFonts += BuildFont;
            UiBuilder.OpenConfigUi += OpenConfigUi;

            CommandManager.AddHandler("/sezzui", new(PluginCommand)
            {
                HelpMessage = "Opens the SezzUI configuration window.",
                ShowInHelp = true
            });
            CommandInfo alias = new(PluginCommand) { ShowInHelp = false };
            CommandManager.AddHandler("/sezz", alias);
            CommandManager.AddHandler("/sui", alias);

            var configManager = ConfigurationManager.Instance;
            var config = configManager.GetConfigObject<DeveloperConfig>();
            if (config != null && config.ShowConfigurationOnLogin)
            {
                ConfigurationManager.Instance.ToggleConfigWindow();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void BuildFont()
        {
            DelvUI.Helpers.FontsManager.Instance.BuildFonts();
        }

        private void LoadBanner()
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
                    PluginLog.Log($"Image failed to load. {bannerImage}");
                    PluginLog.Log(ex.ToString());
                }
            }
            else
            {
                PluginLog.Log($"Image doesn't exist. {bannerImage}");
            }
        }

        private void PluginCommand(string command, string arguments)
        {
            var configManager = ConfigurationManager.Instance;

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

                    case { } argument when argument.StartsWith("profile"):
                        // TODO: Turn this into a helper function?
                        var profile = argument.Split(" ", 2);

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

        private void Draw()
        {
            bool hudState =
                Condition[ConditionFlag.WatchingCutscene] ||
                Condition[ConditionFlag.WatchingCutscene78] ||
                Condition[ConditionFlag.OccupiedInCutSceneEvent] ||
                Condition[ConditionFlag.CreatingCharacter] ||
                Condition[ConditionFlag.BetweenAreas] ||
                Condition[ConditionFlag.BetweenAreas51] ||
                Condition[ConditionFlag.OccupiedSummoningBell];

            UiBuilder.OverrideGameCursor = false;

            ConfigurationManager.Instance.Draw();

            var fontPushed = DelvUI.Helpers.FontsManager.Instance.PushDefaultFont();

            ModuleManager.Draw();

            if (!hudState)
            {
                _hudManager?.Draw();
            }

            if (fontPushed)
            {
                ImGui.PopFont();
            }
        }

        private void OpenConfigUi()
        {
            ConfigurationManager.Instance.ToggleConfigWindow();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }

            _hudManager.Dispose();
            ModuleManager.Instance.Dispose();
            EventManager.Instance.Dispose();

            ConfigurationManager.Instance.SaveConfigurations(true);
            ConfigurationManager.Instance.CloseConfigWindow();

            CommandManager.RemoveHandler("/sezzui");
            CommandManager.RemoveHandler("/sezz");
            CommandManager.RemoveHandler("/sui");

            UiBuilder.Draw -= Draw;
            UiBuilder.BuildFonts -= BuildFont;
            UiBuilder.OpenConfigUi -= OpenConfigUi;
            UiBuilder.RebuildFonts();

            DelvUI.Helpers.ClipRectsHelper.Instance.Dispose();
            DelvUI.Helpers.FontsManager.Instance.Dispose();
            GlobalColors.Instance.Dispose();
            ProfilesManager.Instance.Dispose();
            DelvUI.Helpers.SpellHelper.Instance.Dispose();
            DelvUI.Helpers.TexturesCache.Instance.Dispose();
            Helpers.ImageCache.Instance.Dispose();
            DelvUI.Helpers.TooltipsHelper.Instance.Dispose();

            // This needs to remain last to avoid race conditions
            ConfigurationManager.Instance.Dispose();
        }
    }
}
