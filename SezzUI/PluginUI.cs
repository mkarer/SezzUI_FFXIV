using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;
using System;
using System.Numerics;
using Dalamud.Interface;

namespace SezzUI
{
    // It is good to have this be disposable in general, in case you ever need it
    // to do any cleanup
    class PluginUI : IDisposable
    {
        private SezzUIPluginConfiguration configuration;
        private readonly Animator.Animator _animatorBanner;
        public bool DisplayBanner = false;

        // this extra bool exists for ImGui, since you can't ref a property
        private bool visible = false;
        public bool Visible
        {
            get { return this.visible; }
            set { this.visible = value; }
        }

        private bool settingsVisible = false;
        public bool SettingsVisible
        {
            get { return this.settingsVisible; }
            set { this.settingsVisible = value; }
        }

        // passing in the image here just for simplicity
        public PluginUI(SezzUIPluginConfiguration configuration)
        {
            this.configuration = configuration;

            _animatorBanner = new();
            _animatorBanner.Timelines.OnShow.Chain(new Animator.FadeAnimation(0, 1, 2000));
            _animatorBanner.Timelines.Loop.Chain(new Animator.FadeAnimation(1, 0.6f, 3000));
            _animatorBanner.Timelines.Loop.Chain(new Animator.FadeAnimation(0.6f, 1, 3000));
            _animatorBanner.Timelines.OnHide.Chain(new Animator.FadeAnimation(1, 0, 2000));
        }

        public void Dispose()
        {
        }

        public void Draw()
        {
            if (!DelvUI.Helpers.FontsManager.Instance.DefaultFontBuilt)
            {
                Plugin.UiBuilder.RebuildFonts();
            }

            DelvUI.Helpers.ClipRectsHelper.Instance.Update();

            ImGuiHelpers.ForceNextWindowMainViewport();
            ImGui.SetNextWindowPos(Vector2.Zero);
            ImGui.SetNextWindowSize(ImGui.GetMainViewport().Size);

            var begin = ImGui.Begin(
                "SezzUI_Overlay",
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

            var origin = ImGui.GetMainViewport().Size / 2f;

            // SezzUI
            if (DisplayBanner) DrawBanner(origin);
            DrawOverlay(origin);

            // Template stuff...
            DrawMainWindow();
            DrawSettingsWindow();

            // Done
            ImGui.End();
        }

        private void DrawMainWindow()
        {
            if (!Visible)
            {
                return;
            }

            ImGui.SetNextWindowSize(new Vector2(375, 330), ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowSizeConstraints(new Vector2(375, 330), new Vector2(float.MaxValue, float.MaxValue));
            if (ImGui.Begin("My Amazing Window", ref this.visible, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
            {
                ImGui.Text($"The random config bool is {this.configuration.autoDismount}");

                if (ImGui.Button("Show Settings"))
                {
                    SettingsVisible = true;
                }
            }
            ImGui.End();
        }

        private void DrawSettingsWindow()
        {
            if (!SettingsVisible)
            {
                return;
            }

            ImGui.SetNextWindowSize(new Vector2(232, 75), ImGuiCond.Always);
            if (ImGui.Begin("A Wonderful Configuration Window", ref this.settingsVisible,
                ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
            {
                // can't ref a property, so use a local copy
                var configValue = this.configuration.autoDismount;
                if (ImGui.Checkbox("Random Config Bool", ref configValue))
                {
                    this.configuration.autoDismount = configValue;
                    // can save immediately on change, if you don't want to provide a "Save and Close" button
                    this.configuration.Save();
                }
            }
            ImGui.End();
        }

        #region Banner
        private void DrawBanner(Vector2 origin)
        {
            if (!_animatorBanner.IsAnimating) // There's currently no option to toggle the banner!
                _animatorBanner.Animate();

            _animatorBanner.Update();

            Vector2 elementAnchor = new((float)Math.Floor(ImGui.GetMainViewport().Size.X * 0.64f), ImGui.GetMainViewport().Size.Y - 5);
            Vector2 elementSize = new(100, 32);
            Vector2 elementPosition = DelvUI.Helpers.Utils.GetAnchoredPosition(elementAnchor, elementSize, DelvUI.Enums.DrawAnchor.BottomLeft);

            DelvUI.Helpers.DrawHelper.DrawInWindow("SezzUI_Banner", elementPosition, elementSize, false, false, (drawList) =>
            {
                // Draw Background
                drawList.AddRectFilled(elementPosition, elementPosition + elementSize, ImGui.ColorConvertFloat4ToU32(new Vector4(0, 0, 0, 0.5f * _animatorBanner.Data.Opacity)), 0);

                // Draw Border
                drawList.AddRect(elementPosition, elementPosition + elementSize, ImGui.ColorConvertFloat4ToU32(new Vector4(1, 1, 1, 0.3f * _animatorBanner.Data.Opacity)), 0, ImDrawFlags.None, 1);

                // Draw Text
                bool fontPushed = DelvUI.Helpers.FontsManager.Instance.PushFont("MyriadProLightCond_20");

                string text = "SezzUI";
                Vector2 textSize = ImGui.CalcTextSize(text);
                Vector2 textPosition = DelvUI.Helpers.Utils.GetAnchoredPosition(elementPosition + elementSize / 2, textSize, DelvUI.Enums.DrawAnchor.Center);
                textPosition.Y += 1;

                string textPart1 = "Sezz";
                Vector2 textSizePart1 = ImGui.CalcTextSize(textPart1);
                DelvUI.Helpers.DrawHelper.DrawShadowText(textPart1, textPosition, ImGui.ColorConvertFloat4ToU32(new Vector4((float)1/255, (float)182/255, (float)214/255, _animatorBanner.Data.Opacity)), ImGui.ColorConvertFloat4ToU32(new Vector4(0, 0, 0, _animatorBanner.Data.Opacity)), drawList, 1);

                string textPart2 = "UI";
                Vector2 textPositionPart2 = textPosition;
                textPositionPart2.X += textSizePart1.X;
                DelvUI.Helpers.DrawHelper.DrawShadowText(textPart2, textPositionPart2, ImGui.ColorConvertFloat4ToU32(new Vector4(1, 1, 1, _animatorBanner.Data.Opacity)), ImGui.ColorConvertFloat4ToU32(new Vector4(0, 0, 0, _animatorBanner.Data.Opacity)), drawList, 1);

                if (fontPushed) { ImGui.PopFont(); }
            });
        }
        #endregion

        #region Game Overlay
        protected unsafe bool ShouldShowOverlay()
        {
            if (Plugin.ClientState.LocalPlayer == null)
            {
                return false;
            }

            var parameterWidget = (AtkUnitBase*)Plugin.GameGui.GetAddonByName("_ParameterWidget", 1);
            var fadeMiddleWidget = (AtkUnitBase*)Plugin.GameGui.GetAddonByName("FadeMiddle", 1);

            var paramenterVisible = parameterWidget != null && parameterWidget->IsVisible;
            var fadeMiddleVisible = fadeMiddleWidget != null && fadeMiddleWidget->IsVisible;

            return paramenterVisible && !fadeMiddleVisible;
        }

        private void DrawOverlay(Vector2 origin)
        {
            // Draw overlay 
            if (!ShouldShowOverlay()) return;

            Plugin.SezzUIPlugin.Modules.JobHud.Draw(origin);
        }
        #endregion
    }
}
