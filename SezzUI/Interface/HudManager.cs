using Dalamud.Game.ClientState.Conditions;
using Dalamud.Interface;
using SezzUI.Enums;
using SezzUI.Config;
using DelvUI.Helpers;
using SezzUI.Interface.GeneralElements;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace SezzUI.Interface
{
    public class HudManager : IDisposable
    {
        private GridConfig? _gridConfig;
        private HUDOptionsConfig? _hudOptions;
        private DraggableHudElement? _selectedElement = null;

        private List<HudModule> _hudModules = null!;
        private List<DraggableHudElement> _hudElements = null!;
        private List<IHudElementWithActor> _hudElementsUsingPlayer = null!;
        private List<IHudElementWithActor> _hudElementsUsingTarget = null!;
        private List<IHudElementWithActor> _hudElementsUsingTargetOfTarget = null!;
        private List<IHudElementWithActor> _hudElementsUsingFocusTarget = null!;
        private List<IHudElementWithPreview> _hudElementsWithPreview = null!;

        internal static Modules.JobHud.JobHud? JobHud;
        internal static Modules.CooldownHud.CooldownHud? CooldownHud;
        internal static Modules.GameUI.ElementHider? ElementHider;
        internal static Modules.GameUI.ActionBar? ActionBar;

        private HudHelper _hudHelper = new HudHelper();

        #region Singleton
        public static void Initialize() { Instance = new HudManager(); }

        public static HudManager Instance { get; private set; } = null!;

        public HudManager()
        {
            ConfigurationManager.Instance.ResetEvent += OnConfigReset;
            ConfigurationManager.Instance.LockEvent += OnHUDLockChanged;
            ConfigurationManager.Instance.ConfigClosedEvent += OnConfingWindowClosed;

            CreateHudElements();
        }

        ~HudManager()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }

            _hudHelper.Dispose();

            JobHud?.Dispose();

            _hudModules.ForEach(module => module.Dispose());
            _hudModules.Clear();

            _hudElements.Clear();
            _hudElementsUsingPlayer.Clear();
            _hudElementsUsingTarget.Clear();
            _hudElementsUsingTargetOfTarget.Clear();
            _hudElementsUsingFocusTarget.Clear();

            ConfigurationManager.Instance.ResetEvent -= OnConfigReset;
            ConfigurationManager.Instance.LockEvent -= OnHUDLockChanged;

            Instance = null!;
        }
        #endregion

        private void OnConfigReset(ConfigurationManager sender)
        {
            CreateHudElements();
        }

        private void OnHUDLockChanged(ConfigurationManager sender)
        {
            var draggingEnabled = !sender.LockHUD;

            foreach (var element in _hudElements)
            {
                element.DraggingEnabled = draggingEnabled;
                element.Selected = false;
            }

            _selectedElement = null;
        }

        private void OnConfingWindowClosed(ConfigurationManager sender)
        {
            if (_hudOptions == null || !_hudOptions.AutomaticPreviewDisabling)
            {
                return;
            }

            foreach (IHudElementWithPreview element in _hudElementsWithPreview)
            {
                element.StopPreview();
            }
        }

        private void OnDraggableElementSelected(DraggableHudElement sender)
        {
            foreach (var element in _hudElements)
            {
                element.Selected = element == sender;
            }

            _selectedElement = sender;
        }

        private void CreateHudElements()
        {
            _gridConfig = ConfigurationManager.Instance.GetConfigObject<GridConfig>();
            _hudOptions = ConfigurationManager.Instance.GetConfigObject<HUDOptionsConfig>();

            _hudModules = new();
            _hudElements = new List<DraggableHudElement>();
            _hudElementsUsingPlayer = new List<IHudElementWithActor>();
            _hudElementsUsingTarget = new List<IHudElementWithActor>();
            _hudElementsUsingTargetOfTarget = new List<IHudElementWithActor>();
            _hudElementsUsingFocusTarget = new List<IHudElementWithActor>();
            _hudElementsWithPreview = new List<IHudElementWithPreview>();

            CreateModules();
            CreateMiscElements();

            foreach (var element in _hudElements)
            {
                element.SelectEvent += OnDraggableElementSelected;
            }
        }

        private void CreateMiscElements()
        {
            // Job HUD
            if (JobHud == null)
            {
                JobHud = new(ConfigurationManager.Instance.GetConfigObject<JobHudConfig>(), "Job HUD");
            }
            _hudElements.Add(JobHud);
            _hudElementsUsingPlayer.Add(JobHud);
        }

        private void CreateModules()
        {
            // Cooldown HUD
            if (CooldownHud == null)
            {
                CooldownHud = Modules.CooldownHud.CooldownHud.Initialize();
            }
            _hudModules.Add(CooldownHud);

            // Game UI Tweaks
            if (ActionBar == null)
            {
                // Load this module before ActionBars are getting hidden.
                Modules.GameUI.ActionBar.Initialize();
                ActionBar = Modules.GameUI.ActionBar.Instance;
            }
            _hudModules.Add(ActionBar);

            if (ElementHider == null)
            {
                Modules.GameUI.ElementHider.Initialize();
                ElementHider = Modules.GameUI.ElementHider.Instance;
            }
            _hudModules.Add(ElementHider);
        }

        public void Draw(DrawState drawState)
        {
            if (!FontsManager.Instance.DefaultFontBuilt)
            {
                Plugin.UiBuilder.RebuildFonts();
            }

            TooltipsHelper.Instance.RemoveTooltip(); // remove tooltip from previous frame

            // don't draw hud when it's not supposed to be visible
            if (drawState == DrawState.HiddenNotInGame || drawState == DrawState.HiddenDisabled)
            {
                _hudModules.ForEach(module => module.Draw(drawState, null));
                return;
            }

            ClipRectsHelper.Instance.Update();

            ImGuiHelpers.ForceNextWindowMainViewport();
            ImGui.SetNextWindowPos(Vector2.Zero);
            ImGui.SetNextWindowSize(ImGui.GetMainViewport().Size);

            var begin = ImGui.Begin(
                "SezzUI_HUD",
                ImGuiWindowFlags.NoTitleBar
              | ImGuiWindowFlags.NoScrollbar
              | ImGuiWindowFlags.AlwaysAutoResize
              | ImGuiWindowFlags.NoBackground
              | ImGuiWindowFlags.NoInputs
              | ImGuiWindowFlags.NoBringToFrontOnFocus
              | ImGuiWindowFlags.NoSavedSettings
            );

            if (!begin)
            {
                ImGui.End();
                return;
            }

            _hudHelper.Update();

            AssignActors();

            var origin = ImGui.GetMainViewport().Size / 2f;
            if (_hudOptions is { UseGlobalHudShift: true })
            {
                origin += _hudOptions.HudOffset;
            }

            // don't draw grid during cutscenes or quest events
            if (drawState == DrawState.HiddenCutscene || drawState == DrawState.Partially)
            {
                _hudModules.ForEach(module => module.Draw(drawState, null));
                ImGui.End();
                return;
            }

            // draw grid
            if (_gridConfig is not null && _gridConfig.Enabled)
            {
                DraggablesHelper.DrawGrid(_gridConfig, _hudOptions, _selectedElement);
            }

            // draw modules
            _hudModules.ForEach(module => module.Draw(drawState, origin));

            // draw elements
            lock (_hudElements)
            {
                DraggablesHelper.DrawElements(origin, _hudHelper, _hudElements, _selectedElement);
            }

            // draw tooltip
            TooltipsHelper.Instance.Draw();

            ImGui.End();
        }

        private void AssignActors()
        {
            // player
            var player = Plugin.ClientState.LocalPlayer;
            foreach (var element in _hudElementsUsingPlayer)
            {
                element.Actor = player;
            }

            // target
            var target = Plugin.TargetManager.SoftTarget ?? Plugin.TargetManager.Target;
            foreach (var element in _hudElementsUsingTarget)
            {
                element.Actor = target;
            }

            // target of target
            var targetOfTarget = Utils.FindTargetOfTarget(target, player, Plugin.ObjectTable);
            foreach (var element in _hudElementsUsingTargetOfTarget)
            {
                element.Actor = targetOfTarget;
            }

            // focus
            var focusTarget = Plugin.TargetManager.FocusTarget;
            foreach (var element in _hudElementsUsingFocusTarget)
            {
                element.Actor = focusTarget;
            }
        }
    }

    internal static class HUDConstants
    {
        internal static int BaseHUDOffsetY = (int)(ImGui.GetMainViewport().Size.Y * 0.3f);
        internal static int UnitFramesOffsetX = 160;
        internal static int PlayerCastbarY = BaseHUDOffsetY - 13;
        internal static int JobHudsBaseY = PlayerCastbarY - 14;
        internal static Vector2 DefaultBigUnitFrameSize = new Vector2(270, 50);
        internal static Vector2 DefaultSmallUnitFrameSize = new Vector2(120, 20);
        internal static Vector2 DefaultStatusEffectsListSize = new Vector2(292, 82);
    }
}
