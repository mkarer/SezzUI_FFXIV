using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
using Dalamud.Interface;
using Dalamud.Interface.Windowing;
using Dalamud.Game.Gui;
using Dalamud.Plugin;
using SigScanner = Dalamud.Game.SigScanner;
using Dalamud.Logging;
using ImGuiNET;

namespace SezzUI
{
    public sealed class Plugin : IDalamudPlugin
    {
        public static string AssemblyLocation { get; private set; } = "";
        public string Name => "SezzUI";

        private const string commandName = "/sezz";

        public static Plugin SezzUIPlugin { get; private set; } = null!;
     
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

        private SezzUIPluginConfiguration Configuration { get; init; }
        private PluginUI PluginUi { get; init; }
        //public static Plugin Plugin { get; private set; }

        public AvailableEvents Events = new AvailableEvents();
        public AvailableModules Modules = new AvailableModules();
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
            TargetManager targetManager)
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

            DelvUI.Helpers.FontsManager.Initialize(AssemblyLocation);
            DelvUI.Helpers.TexturesCache.Initialize();
            Helpers.ImageCache.Initialize();

            pluginInterface.Create<Service>();
            DelvUI.Helpers.ClipRectsHelper.Initialize();

            PluginInterface = pluginInterface;
            SezzUIPlugin = this;
            CommandManager = commandManager;
            Configuration = PluginInterface.GetPluginConfig() as SezzUIPluginConfiguration ?? new SezzUIPluginConfiguration();
            Configuration.Initialize(PluginInterface);

            // you might normally want to embed resources and load them from the manifest stream
            var assemblyLocation = Assembly.GetExecutingAssembly().Location;
            var imagePath = Path.Combine(Path.GetDirectoryName(assemblyLocation)!, "goat.png");
            var goatImage = PluginInterface.UiBuilder.LoadImage(imagePath);
            PluginUi = new PluginUI(Configuration, goatImage);

            CommandManager.AddHandler(commandName, new CommandInfo(OnCommand)
            {
                HelpMessage = "Open Configuration"
            });

            PluginInterface.UiBuilder.Draw += Draw;
            PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;
            UiBuilder.BuildFonts += BuildFont;
            
            Modules.JobHud.Enable();
        }

        public void Dispose()
        {
            PluginLog.Debug(string.Format("[Core] Dispose"));

            Modules.Dispose();
            Events.Dispose();

            PluginUi.Dispose();
            CommandManager.RemoveHandler(commandName);

            PluginInterface.UiBuilder.Draw -= Draw;
            PluginInterface.UiBuilder.OpenConfigUi -= DrawConfigUI;
            UiBuilder.BuildFonts -= BuildFont;
            UiBuilder.RebuildFonts();

            DelvUI.Helpers.ClipRectsHelper.Instance.Dispose();
            DelvUI.Helpers.FontsManager.Instance.Dispose();
            DelvUI.Helpers.TexturesCache.Instance.Dispose();
            Helpers.ImageCache.Instance.Dispose();
        }

        private void OnCommand(string command, string args)
        {
            // in response to the slash command, just display our main ui
            PluginUi.Visible = true;
        }

        private void Draw()
        {
            UiBuilder.OverrideGameCursor = false;

            var fontPushed = DelvUI.Helpers.FontsManager.Instance.PushDefaultFont();

            PluginUi.Draw();

            if (fontPushed)
            {
                ImGui.PopFont();
            }
        }

        private void DrawConfigUI()
        {
            PluginUi.SettingsVisible = true;
        }

        private void BuildFont()
        {
            DelvUI.Helpers.FontsManager.Instance.BuildFonts();
        }
    }
}
