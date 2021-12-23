using Dalamud.Game.ClientState.Conditions;
using Dalamud.Interface;
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

        private List<DraggableHudElement> _hudElements = null!;
        private List<IHudElementWithActor> _hudElementsUsingPlayer = null!;
        private List<IHudElementWithActor> _hudElementsUsingTarget = null!;
        private List<IHudElementWithActor> _hudElementsUsingTargetOfTarget = null!;
        private List<IHudElementWithActor> _hudElementsUsingFocusTarget = null!;
        private List<IHudElementWithPreview> _hudElementsWithPreview = null!;

        internal static Modules.JobHud.JobHud? JobHud;

        private double _occupiedInQuestStartTime = -1;

        private HudHelper _hudHelper = new HudHelper();

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

            if (JobHud != null) { JobHud.Dispose(); }

            _hudElements.Clear();
            _hudElementsUsingPlayer.Clear();
            _hudElementsUsingTarget.Clear();
            _hudElementsUsingTargetOfTarget.Clear();
            _hudElementsUsingFocusTarget.Clear();

            ConfigurationManager.Instance.ResetEvent -= OnConfigReset;
            ConfigurationManager.Instance.LockEvent -= OnHUDLockChanged;
        }

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

            _hudElements = new List<DraggableHudElement>();
            _hudElementsUsingPlayer = new List<IHudElementWithActor>();
            _hudElementsUsingTarget = new List<IHudElementWithActor>();
            _hudElementsUsingTargetOfTarget = new List<IHudElementWithActor>();
            _hudElementsUsingFocusTarget = new List<IHudElementWithActor>();
            _hudElementsWithPreview = new List<IHudElementWithPreview>();

            CreateMiscElements();

            foreach (var element in _hudElements)
            {
                element.SelectEvent += OnDraggableElementSelected;
            }
        }

        private void CreateMiscElements()
        {
            if (JobHud == null) {
                JobHud = new(ConfigurationManager.Instance.GetConfigObject<JobHudConfig>(), "Job HUD");
            }
            _hudElements.Add(JobHud);
            _hudElementsUsingPlayer.Add(JobHud);
        }

        public void Draw()
        {
            if (!FontsManager.Instance.DefaultFontBuilt)
            {
                Plugin.UiBuilder.RebuildFonts();
            }

            TooltipsHelper.Instance.RemoveTooltip(); // remove tooltip from previous frame

            if (!ShouldBeVisible())
            {
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

            // show only castbar during quest events
            if (ShouldOnlyShowCastbar())
            {
                ImGui.End();
                return;
            }

            // grid
            if (_gridConfig is not null && _gridConfig.Enabled)
            {
                DraggablesHelper.DrawGrid(_gridConfig, _hudOptions, _selectedElement);
            }

            // draw elements
            lock (_hudElements)
            {
                DraggablesHelper.DrawElements(origin, _hudHelper, _hudElements, _selectedElement);
            }

            // tooltip
            TooltipsHelper.Instance.Draw();

            ImGui.End();
        }

        protected unsafe bool ShouldBeVisible()
        {
            if (!ConfigurationManager.Instance.ShowHUD || Plugin.ClientState.LocalPlayer == null)
            {
                return false;
            }

            var parameterWidget = (AtkUnitBase*)Plugin.GameGui.GetAddonByName("_ParameterWidget", 1);
            var fadeMiddleWidget = (AtkUnitBase*)Plugin.GameGui.GetAddonByName("FadeMiddle", 1);

            var paramenterVisible = parameterWidget != null && parameterWidget->IsVisible;
            var fadeMiddleVisible = fadeMiddleWidget != null && fadeMiddleWidget->IsVisible;

            return paramenterVisible && !fadeMiddleVisible;
        }

        protected bool ShouldOnlyShowCastbar()
        {
            // when in quest dialogs and events, hide everything except castbars
            // this includes talking to npcs or interacting with quest related stuff
            if (Plugin.Condition[ConditionFlag.OccupiedInQuestEvent] ||
                Plugin.Condition[ConditionFlag.OccupiedInEvent])
            {
                // we have to wait a bit to avoid weird flickering when clicking shiny stuff
                // we hide the ui after half a second passed in this state
                // interestingly enough, default hotbars seem to do something similar
                var time = ImGui.GetTime();
                if (_occupiedInQuestStartTime > 0)
                {
                    if (time - _occupiedInQuestStartTime > 0.5)
                    {
                        return true;
                    }
                }
                else
                {
                    _occupiedInQuestStartTime = time;
                }
            }
            else
            {
                _occupiedInQuestStartTime = -1;
            }

            return false;
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
