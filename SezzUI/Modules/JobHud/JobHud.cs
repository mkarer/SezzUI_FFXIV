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

        private Animator.Animator _animator = new();
        public bool IsShown { get { return _isShown; } }
        internal bool _isShown = false;
        private Vector2 _positionOffset = new Vector2(0, 148);

        private static readonly Lazy<JobHud> ev = new Lazy<JobHud>(() => new JobHud());
        public static JobHud Instance { get { return ev.Value; } }
        public static bool Initialized { get { return ev.IsValueCreated; } }

        private List<Bar> _bars = new();
        private List<AuraAlert> _auraAlerts = new();

        public JobHud()
		{
            _animator.Timelines.OnShow.Data.DefaultOpacity = 0;
            _animator.Timelines.OnShow.Chain(new Animator.FadeAnimation(0, 1, 150));
            _animator.Timelines.OnHide.Data.DefaultOpacity = 1;
            _animator.Timelines.OnHide.Chain(new Animator.FadeAnimation(1, 0, 500));
        }

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

                if (Plugin.SezzUIPlugin.Events.Combat.IsInCombat())
				{
                    Show();
                }
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
                Reset();

                Service.ClientState.Login -= OnLogin;
                Service.ClientState.Logout -= OnLogout;

                Plugin.SezzUIPlugin.Events.Player.JobChanged -= OnJobChanged;
                Plugin.SezzUIPlugin.Events.Combat.EnteringCombat -= OnEnteringCombat;
                Plugin.SezzUIPlugin.Events.Combat.LeavingCombat -= OnLeavingCombat;

                OnLogout(null!, null!);
                base.Disable();
            }
            else
            {
                PluginLog.Debug($"[HudModule:{Name}] Disable skipped");
            }
        }

        private void Reset()
		{
            Hide(true);
            _bars.ForEach(bar => bar.Dispose());
            _bars.Clear();
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
                case DelvUI.Helpers.JobIDs.GNB:
                    // Offensives/Utility/Small Defensives
                    using (Bar bar = new())
                    {
                        bar.Add(new Icon { TextureActionId = 16138, CooldownActionId = 16138, StatusId = 1831, MaxStatusDuration = 20, StatusTarget = Enums.Unit.Player, Level = 2 }); // No Mercy
                        bar.Add(new Icon { TextureActionId = 16154, CooldownActionId = 16154, Level = 56 }); // Rough Divide
                        bar.Add(new Icon { TextureActionId = 16151, CooldownActionId = 16151, StatusId = 1835, MaxStatusDuration = 18, StatusTarget = Enums.Unit.Player, Level = 45 }); // Aurora
                        bar.Add(new Icon { TextureActionId = 16161, CooldownActionId = 16161, StatusId = 1840, MaxStatusDuration = 7, StatusTarget = Enums.Unit.Player, Level = 68 }); // Heart of Stone
                        bar.Add(new Icon { TextureActionId = 16160, CooldownActionId = 16160, StatusId = 1839, MaxStatusDuration = 15, StatusTarget = Enums.Unit.Player, Level = 64 }); // Heart of Light
                        if (bar.HasIcons) _bars.Add(bar);
                    }

                    // Big Defensives
                    using (Bar bar = new())
                    {
                        bar.Add(new Icon { TextureActionId = 7531, CooldownActionId = 7531, StatusId = 1191, MaxStatusDuration = 20, StatusTarget = Enums.Unit.Player, Level = 8 }); // Rampart
                        bar.Add(new Icon { TextureActionId = 16140, CooldownActionId = 16140, StatusId = 1832, MaxStatusDuration = 20, StatusTarget = Enums.Unit.Player, Level = 6 }); // Camouflage
                        bar.Add(new Icon { TextureActionId = 16148, CooldownActionId = 16148, StatusId = 1834, MaxStatusDuration = 15, StatusTarget = Enums.Unit.Player, Level = 38 }); // Nebula
                        bar.Add(new Icon { TextureActionId = 16152, CooldownActionId = 16152, StatusId = 1836, MaxStatusDuration = 10, StatusTarget = Enums.Unit.Player, Level = 50 }); // Superbolide
                        if (bar.HasIcons) _bars.Add(bar);
                    }
                    break;

                case DelvUI.Helpers.JobIDs.BLM:
                    // Firestarter
                    _auraAlerts.Add(new AuraAlert
                    {
                        StatusId = 165,
                        Image = Plugin.AssemblyLocation + "Media\\Images\\Overlays\\impact.png",
                        Size = new Vector2(256, 128) * 0.8f,
                        Position = new Vector2(0, -180),
                        MaxDuration = 30
                    });
                    break;

                case DelvUI.Helpers.JobIDs.RPR:
                    // Soulsow/Harvest Moon
                    _auraAlerts.Add(new AuraAlert
                    {
                        StatusId = 2594,
                        InvertCheck = true,
                        EnableInCombat = false,
                        EnableOutOfCombat = true,
                        TreatWeaponOutAsCombat = false,
                        Image = Plugin.AssemblyLocation + "Media\\Images\\Overlays\\surge_of_darkness.png",
                        Size = new Vector2(128, 256),
                        Position = new Vector2(-140, 50)
                    });
                    break;
			}
        }

        public override void Draw(Vector2 origin)
        {
            if (!Enabled) return;

            // Bars
            if (IsShown || _animator.IsAnimating)
            {
                _animator.Update();

                // Debug anchor (for testing animations)
                //Vector2 elementSize = new(100, 32);
                //Vector2 elementPosition = DelvUI.Helpers.Utils.GetAnchoredPosition(origin, elementSize, DelvUI.Enums.DrawAnchor.Center);

                //DelvUI.Helpers.DrawHelper.DrawInWindow("SezzUI_JobHud", elementPosition, elementSize, false, false, (drawList) =>
                //{
                //    Helpers.DrawHelper.DrawPlaceholder(Name, elementPosition, elementSize, drawList, _animator.Data.Opacity);
                //});

                for (int i = 0; i < _bars.Count; i++)
				{
                    Vector2 pos = origin + _positionOffset;
                    pos.Y += i * (_bars[i].IconSize.Y + (float)_bars[i].IconPadding);
                    _bars[i].Draw(pos, _animator);
                }
            }

            // Aura Alerts
            _auraAlerts.ForEach(aa => aa.Draw(origin));
        }

        public void Show()
        {
            if (!IsShown)
            {
                PluginLog.Debug($"[{Name}] Show");
                _isShown = !IsShown;
                _animator.Animate();
            }
        }

        public void Hide(bool force = false)
        {
            if (IsShown)
            {
                PluginLog.Debug($"[{Name}] Hide");
                _isShown = !IsShown;
                _animator.Stop(force);
            }
        }

        private void OnLogin(object? sender, EventArgs e)
        {
            PluginLog.Debug($"[{Name}] OnLogin");
        }

        private void OnLogout(object? sender, EventArgs e)
        {
            PluginLog.Debug($"[{Name}] OnLogout");
            Hide(true);
        }

        private void OnEnteringCombat(object? sender, EventArgs e)
        {
            PluginLog.Debug($"[{Name}] OnEnteringCombat");
            Show();
        }

        private void OnLeavingCombat(object? sender, EventArgs e)
        {
            PluginLog.Debug($"[{Name}] OnLeavingCombat");
            Hide();
        }

        private void OnJobChanged(object? sender, GameEvents.JobChangedEventArgs e)
        {
            PluginLog.Debug($"[{Name}] OnJobChanged: {e.JobId}");
            Configure();
        }

        public override void Dispose()
        {
            Reset();
            base.Dispose();
        }
    }
}
