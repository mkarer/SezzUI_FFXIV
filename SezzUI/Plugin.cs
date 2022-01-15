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
using SezzUI.Enums;
using SezzUI.Config;
using SezzUI.Config.Profiles;
using SezzUI.Interface;
using SezzUI.Interface.GeneralElements;
using ImGuiNET;
using ImGuiScene;
using SigScanner = Dalamud.Game.SigScanner;
using FFXIVClientStructs.FFXIV.Component.GUI;

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
        public static ChatGui ChatGui { get; private set; } = null!;

        public static TextureWrap? BannerTexture;

        public static string AssemblyLocation { get; private set; } = "";
        public string Name => "SezzUI";
        public static string Version { get; private set; } = "";

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
            TargetManager targetManager,
            ChatGui chatGui
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

            Version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0.5";

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
            Helpers.SpellHelper.Initialize();
            DelvUI.Helpers.TooltipsHelper.Initialize();
            EventManager.Initialize();
            HudManager.Initialize();

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

#if DEBUG
            var config = ConfigurationManager.Instance.GetConfigObject<DeveloperConfig>();
            if (config != null && config.ShowConfigurationOnLogin)
            {
                OpenConfigUi();
            }
#endif
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

                    case "test":
                        Core.Test.RunTest();
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
            UiBuilder.OverrideGameCursor = false;
            ConfigurationManager.Instance.Draw();

            if (HudManager.Instance != null)
            {
                bool fontPushed = DelvUI.Helpers.FontsManager.Instance.PushDefaultFont();

                DrawState drawState = GetDrawState();
                if (_lastDrawState != drawState)
                {
                    _lastDrawState = drawState;
#if DEBUG
                    var config = ConfigurationManager.Instance.GetConfigObject<DeveloperConfig>();
                    if (config != null && config.LogEvents && config.LogEventPluginDrawStateChanged)
                    {
                        PluginLog.Debug($"[Plugin::DrawStateChanged] State: {drawState}");
                    }
#endif
                }

                HudManager.Instance.Draw(drawState);

                if (fontPushed)
                {
                    ImGui.PopFont();
                }
            }
        }

        #region Draw State
        //private static double _occupiedInQuestStartTime = -1;
        private DrawState _lastDrawState = DrawState.Unknown;

        public static unsafe DrawState GetDrawState()
        {
            // Dalamud conditions
            if (!ClientState.IsLoggedIn || Condition[ConditionFlag.CreatingCharacter] || Condition[ConditionFlag.BetweenAreas] || Condition[ConditionFlag.BetweenAreas51] || ClientState.LocalPlayer == null)
            {
                return DrawState.HiddenNotInGame;
            }
            else if (!ConfigurationManager.Instance.ShowHUD)
            {
                return DrawState.HiddenDisabled;
            }
            else if (Condition[ConditionFlag.WatchingCutscene] || Condition[ConditionFlag.WatchingCutscene78] || Condition[ConditionFlag.OccupiedInCutSceneEvent])
            {
                return DrawState.HiddenCutscene;
            }
            else if (Condition[ConditionFlag.OccupiedSummoningBell])
            {
                return DrawState.Partially;
            }

            // Quest interaction
            if (Condition[ConditionFlag.OccupiedInQuestEvent] || Condition[ConditionFlag.OccupiedInEvent])
            {
                return DrawState.Partially;
            }

            try
            {
                var parameterWidget = (AtkUnitBase*)GameGui.GetAddonByName("_ParameterWidget", 1);
                if (parameterWidget != null && !parameterWidget->IsVisible)
                {
                    return DrawState.Partially;
                }

                var fadeMiddleWidget = (AtkUnitBase*)GameGui.GetAddonByName("FadeMiddle", 1);
                if (fadeMiddleWidget != null && fadeMiddleWidget->IsVisible)
                {
                    return DrawState.Partially;
                }

                // TODO: Test if this is good enough, remove if a timer is really needed.
                var actionBarWidget = (AtkUnitBase*)GameGui.GetAddonByName("_ActionBar", 1);
                if (actionBarWidget != null && !actionBarWidget->IsVisible)
                {
                    return DrawState.Partially;
                }
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, $"[GetDrawState] Error: {ex}");
            }

            //if (Condition[ConditionFlag.OccupiedInQuestEvent] || Condition[ConditionFlag.OccupiedInEvent])
            //{
            //    // We have to wait a bit to avoid weird flickering when clicking shiny stuff
            //    // and hide the ui after half a second passed in this state.
            //    // Interestingly enough, default hotbars seem to do something similar.
            //    var time = ImGui.GetTime();
            //    if (_occupiedInQuestStartTime > 0)
            //    {
            //        if (time - _occupiedInQuestStartTime >= 0.25) // TODO: Exact duration might be related to ping or other events!
            //        {
            //            return DrawState.PartiallyInteraction;
            //        }
            //    }
            //    else
            //    {
            //        _occupiedInQuestStartTime = time;
            //    }
            //}
            //else
            //{
            //    _occupiedInQuestStartTime = -1;
            //}

            return DrawState.Visible;
        }
        #endregion

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

            HudManager.Instance.Dispose();
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
            Helpers.SpellHelper.Instance.Dispose();
            DelvUI.Helpers.TooltipsHelper.Instance.Dispose();
            if (NativeMethods.Initialized) { NativeMethods.Instance.Dispose(); }

            // This needs to remain last to avoid race conditions
            ConfigurationManager.Instance.Dispose();

            PluginLog.Debug($"Goodbye!");
        }
    }
}
