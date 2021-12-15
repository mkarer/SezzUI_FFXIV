using System;
using ImGuiNET;
using Dalamud.Logging;
using System.Numerics;
using System.Collections.Generic;
using Dalamud.Game;
using Dalamud.Game.ClientState.Objects.SubKinds;

namespace SezzUI.Modules.JobHud
{
    public sealed class JobHud : HudModule
    {
        public override string Name => "Job HUD";
        public override string Description => "Tracks cooldowns, buffs, debuff and proccs.";

        private static readonly Lazy<JobHud> ev = new Lazy<JobHud>(() => new JobHud());
        public static JobHud Instance { get { return ev.Value; } }
        public static bool Initialized { get { return ev.IsValueCreated; } }

        private BarController _barcontroller = new();
        private bool _currentVisibility = false;
        private bool _lastVisibility = false;
        
        public float Opacity = 0f;
        public bool IsFading = false;
        private readonly FadeInfo _fadeInfoShow = new(300, 0, 1);
        private readonly FadeInfo _fadeInfoHide = new(750, 1, 0);

        private List<AuraAlert> _auraAlerts = new();

        public override void Enable()
        {
            if (!Enabled)
            {
                base.Enable();

                Service.ClientState.Login += OnLogin;
                Service.ClientState.Logout += OnLogout;

                Plugin.SezzUIPlugin.Events.Player.JobChanged += OnJobChanged;
                Plugin.SezzUIPlugin.Events.Combat.EnteringCombat += OnEnteringCombat;
                Plugin.SezzUIPlugin.Events.Combat.LeavingCombat += OnLeavingCombat;

                Configure();

                _currentVisibility = Plugin.SezzUIPlugin.Events.Combat.IsInCombat();
            }
            else
            {
                PluginLog.Debug($"[HudModule:{Name}] Enable skipped");
            }

        }

        public override void Disable()
        {
            if (Enabled)
            {
                base.Disable();

                Reset();

                Service.ClientState.Login -= OnLogin;
                Service.ClientState.Logout -= OnLogout;

                Plugin.SezzUIPlugin.Events.Player.JobChanged -= OnJobChanged;
                Plugin.SezzUIPlugin.Events.Combat.EnteringCombat -= OnEnteringCombat;
                Plugin.SezzUIPlugin.Events.Combat.LeavingCombat -= OnLeavingCombat;

                _currentVisibility = false;
                _lastVisibility = false;

                OnLogout(null!, null!);
            }
            else
            {
                PluginLog.Debug($"[HudModule:{Name}] Disable skipped");
            }
        }

        private void Reset()
		{
            _auraAlerts.ForEach(aa => aa.Dispose());
            _auraAlerts.Clear();
        }

        private void Configure()
		{
            Reset();

            PlayerCharacter? player = Service.ClientState.LocalPlayer;
            uint jobId = (player != null ? player.ClassJob.Id : 0);

            switch (jobId)
			{
                case DelvUI.Helpers.JobIDs.RPR:
                    _auraAlerts.Add(new AuraAlert()); // Soulsow (Harvest Moon)
                    break;
			}
        }

        private void UpdateOpacity()
        {
            if (_lastVisibility != _currentVisibility)
            {
                // Visibility changed, enable fading
                _lastVisibility = _currentVisibility;
                if (_currentVisibility)
                {
                    // Fade in
                    PluginLog.Debug($"[{Name}] Fade IN");
                    _fadeInfoShow.Start(true);
                }
                else
                {
                    // Fade out
                    PluginLog.Debug($"[{Name}] Fade OUT");
                    _fadeInfoHide.Start(true);
                }
            }

            if (_currentVisibility)
            {
                if (_fadeInfoShow.IsActive)
                {
                    IsFading = true;
                    Opacity = _fadeInfoShow.GetOpacity();
                }
                else
                {
                    IsFading = false;
                    Opacity = _fadeInfoShow.MaxOpacity;
                }
            }
            else
            {
                if (_fadeInfoHide.IsActive)
                {
                    IsFading = true;
                    Opacity = _fadeInfoHide.GetOpacity();
                }
                else
                {
                    IsFading = false;
                    Opacity = _fadeInfoHide.MinOpacity;
                }
            }
        }

        private bool _showDebugAnchor = false;

        public override void Draw(Vector2 origin)
        {
            if (!Enabled) return;

            // Bars
            UpdateOpacity();
            if (_currentVisibility || IsFading)
            {
                if (_showDebugAnchor)
				{
                    Vector2 elementSize = new(100, 32);
                    Vector2 elementPosition = DelvUI.Helpers.Utils.GetAnchoredPosition(origin, elementSize, DelvUI.Enums.DrawAnchor.Center);

                    DelvUI.Helpers.DrawHelper.DrawInWindow("SezzUI_JobHud", elementPosition, elementSize, false, false, (drawList) =>
                    {
                        // Draw Background
                        drawList.AddRectFilled(elementPosition, elementPosition + elementSize, ImGui.ColorConvertFloat4ToU32(new Vector4(0, 0, 0, 0.5f * Opacity)), 0);

                        // Draw Border
                        drawList.AddRect(elementPosition, elementPosition + elementSize, ImGui.ColorConvertFloat4ToU32(new Vector4(1, 1, 1, 0.3f * Opacity)), 0, ImDrawFlags.None, 1);

                        // Draw Text
                        bool fontPushed = DelvUI.Helpers.FontsManager.Instance.PushFont("MyriadProLightCond_16");

                        string text = Name;
                        Vector2 textSize = ImGui.CalcTextSize(text);
                        Vector2 textPosition = DelvUI.Helpers.Utils.GetAnchoredPosition(elementPosition + elementSize / 2, textSize, DelvUI.Enums.DrawAnchor.Center);
                        textPosition.Y += 1;
                        DelvUI.Helpers.DrawHelper.DrawShadowText(text, textPosition, ImGui.ColorConvertFloat4ToU32(new Vector4(1, 1, 1, Opacity)), ImGui.ColorConvertFloat4ToU32(new Vector4(0, 0, 0, Opacity)), drawList, 1);

                        if (fontPushed) { ImGui.PopFont(); }
                    });
				}
            }

            // Aura Alerts
            _auraAlerts.ForEach(aa => aa.Draw(origin));
        }

        private void OnLogin(object? sender, EventArgs e)
        {
            PluginLog.Debug($"[{Name}] OnLogin");
        }

        private void OnLogout(object? sender, EventArgs e)
        {
            PluginLog.Debug($"[{Name}] OnLogout");
        }

        private void OnEnteringCombat(object? sender, EventArgs e)
        {
            PluginLog.Debug($"[{Name}] OnEnteringCombat");
            _currentVisibility = true;
        }

        private void OnLeavingCombat(object? sender, EventArgs e)
        {
            PluginLog.Debug($"[{Name}] OnLeavingCombat");
            _currentVisibility = false;
        }

        private void OnJobChanged(object? sender, GameEvents.JobChangedEventArgs e)
        {
            PluginLog.Debug($"[{Name}] OnJobChanged: {e.JobId}");
            Configure();
        }
    }
}
