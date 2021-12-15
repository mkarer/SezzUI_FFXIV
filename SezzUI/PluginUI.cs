using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;
using System;
using System.Numerics;
using Dalamud.Interface;

namespace SezzUI
{
    #region Fading
    
    public class FadeInfo
    {
        public uint Duration;
        public float MinOpacity;
        public float MaxOpacity;
        private FadeDirection _direction;
        public bool IsActive { get { return !_finished;  } }

        public bool Bounce;
        public uint HoldDuration;

        private int? _ticksFirstDisplay;
        private FadeDirection _currentDirection;
        private bool _finished = true;

        public FadeInfo(uint duration, float from, float to, bool bounce = false, uint hold = 0)
        {
            Duration = (uint)Math.Max(50f, duration);
            MinOpacity = Math.Max(0f, Math.Min(from, to));
            MaxOpacity = Math.Min(1f, Math.Max(from, to));
            _direction = (to > from) ? FadeDirection.In : FadeDirection.Out;
            _currentDirection = _direction;

            Bounce = bounce;
            HoldDuration = hold;
        }

        public void Start(bool reset = false, bool resetDirection = false)
        {
            if (reset || _ticksFirstDisplay == null)
            {
                _ticksFirstDisplay = Environment.TickCount;
                _finished = false;
                if (resetDirection) { _currentDirection = _direction; }
            }
        }

        public float GetOpacity()
        {
            float fadeOpacity = _currentDirection == FadeDirection.In ? MaxOpacity : MinOpacity;

            if (!_finished && _ticksFirstDisplay != null)
            {
                int ticksNow = Environment.TickCount;
                int timeElapsed = ticksNow - (int)_ticksFirstDisplay;

                bool holding = Bounce && timeElapsed >= Duration && timeElapsed < Duration + HoldDuration;

                if (!holding)
                {
                    float fadeFrom = _currentDirection == FadeDirection.In ? MinOpacity : MaxOpacity;
                    float fadeTo = _currentDirection == FadeDirection.In ? MaxOpacity : MinOpacity;

                    float fadeRange = fadeTo - fadeFrom;
                    float fadeProgress = Math.Min(1, Math.Max(0, (float)timeElapsed / (float)Duration));
                    fadeOpacity = fadeFrom + fadeRange * fadeProgress;
                }

                if (timeElapsed >= Duration)
                {
                    if (Bounce && !holding)
                    {
                        // Restart
                        Start(true);
                        _currentDirection = (_currentDirection == FadeDirection.In) ? FadeDirection.Out : FadeDirection.In;
                    } else if (!Bounce)
                    {
                        // Done
                        _finished = true;
                    }
                }
            }

            return fadeOpacity;
        }
    }

    public enum FadeDirection
    {
        In,
        Out
    }

    #endregion

    // It is good to have this be disposable in general, in case you ever need it
    // to do any cleanup
    class PluginUI : IDisposable
    {
        private SezzUIPluginConfiguration configuration;

        private ImGuiScene.TextureWrap goatImage;

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
        public PluginUI(SezzUIPluginConfiguration configuration, ImGuiScene.TextureWrap goatImage)
        {
            this.configuration = configuration;
            this.goatImage = goatImage;
        }

        public void Dispose()
        {
            this.goatImage.Dispose();
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
            DrawBanner(origin);
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

                ImGui.Spacing();

                ImGui.Text("Have a goat:");
                ImGui.Indent(55);
                ImGui.Image(this.goatImage.ImGuiHandle, new Vector2(this.goatImage.Width, this.goatImage.Height));
                ImGui.Unindent(55);
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

        private readonly FadeInfo _fadeInfoBanner = new(2000, 1, 0.5f, true);
     
        private void DrawBanner(Vector2 origin)
        {
            _fadeInfoBanner.Start();
            float fadeOpacity = _fadeInfoBanner.GetOpacity();

            Vector2 elementAnchor = new((float)Math.Floor(ImGui.GetMainViewport().Size.X * 0.64f), ImGui.GetMainViewport().Size.Y - 5);
            Vector2 elementSize = new(100, 32);
            Vector2 elementPosition = DelvUI.Helpers.Utils.GetAnchoredPosition(elementAnchor, elementSize, DelvUI.Enums.DrawAnchor.BottomLeft);

            DelvUI.Helpers.DrawHelper.DrawInWindow("SezzUI_Banner", elementPosition, elementSize, false, false, (drawList) =>
            {
                // Draw Background
                drawList.AddRectFilled(elementPosition, elementPosition + elementSize, ImGui.ColorConvertFloat4ToU32(new Vector4(0, 0, 0, 0.5f * fadeOpacity)), 0);

                // Draw Border
                drawList.AddRect(elementPosition, elementPosition + elementSize, ImGui.ColorConvertFloat4ToU32(new Vector4(1, 1, 1, 0.3f * fadeOpacity)), 0, ImDrawFlags.None, 1);

                // Draw Text
                bool fontPushed = DelvUI.Helpers.FontsManager.Instance.PushFont("MyriadProLightCond_20");

                string text = "SezzUI";
                Vector2 textSize = ImGui.CalcTextSize(text);
                Vector2 textPosition = DelvUI.Helpers.Utils.GetAnchoredPosition(elementPosition + elementSize / 2, textSize, DelvUI.Enums.DrawAnchor.Center);
                textPosition.Y += 1;

                string textPart1 = "Sezz";
                Vector2 textSizePart1 = ImGui.CalcTextSize(textPart1);
                DelvUI.Helpers.DrawHelper.DrawShadowText(textPart1, textPosition, ImGui.ColorConvertFloat4ToU32(new Vector4((float)1/255, (float)182/255, (float)214/255, fadeOpacity)), ImGui.ColorConvertFloat4ToU32(new Vector4(0, 0, 0, fadeOpacity)), drawList, 1);

                string textPart2 = "UI";
                Vector2 textPositionPart2 = textPosition;
                textPositionPart2.X += textSizePart1.X;
                DelvUI.Helpers.DrawHelper.DrawShadowText(textPart2, textPositionPart2, ImGui.ColorConvertFloat4ToU32(new Vector4(1, 1, 1, fadeOpacity)), ImGui.ColorConvertFloat4ToU32(new Vector4(0, 0, 0, fadeOpacity)), drawList, 1);

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
