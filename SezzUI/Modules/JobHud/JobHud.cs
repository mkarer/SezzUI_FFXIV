using System;
using System.Linq;
using System.Reflection;
using Dalamud.Logging;
using System.Numerics;
using System.Collections.Generic;
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
        private Vector2 _positionOffset = new Vector2(0, 150);

        private static readonly Lazy<JobHud> ev = new Lazy<JobHud>(() => new JobHud());
        public static JobHud Instance { get { return ev.Value; } }
        public static bool Initialized { get { return ev.IsValueCreated; } }

        internal Vector4 AccentColor;
        public List<Bar> Bars { get { return _bars; } }
        private List<Bar> _bars = new();
        private List<AuraAlert> _auraAlerts = new();
        private Dictionary<uint, BasePreset> _presets = new();

        public JobHud()
		{
            _animator.Timelines.OnShow.Data.DefaultOpacity = 0;
            _animator.Timelines.OnShow.Data.DefaultOffset.Y = -20;
            _animator.Timelines.OnShow.Add(new Animator.FadeAnimation(0, 1, 150));
            _animator.Timelines.OnShow.Add(new Animator.TranslationAnimation(_animator.Timelines.OnShow.Data.DefaultOffset, Vector2.Zero, 150));

            _animator.Timelines.OnHide.Data.DefaultOpacity = 1;
            _animator.Timelines.OnHide.Add(new Animator.FadeAnimation(1, 0, 150));
            _animator.Timelines.OnHide.Add(new Animator.TranslationAnimation(Vector2.Zero, new Vector2(0, 20), 150));

            try
            {
                Assembly.GetAssembly(typeof(BasePreset))!.GetTypes().Where(t => t.BaseType == typeof(BasePreset)).Select(t => Activator.CreateInstance(t)).Cast<BasePreset>().ToList().ForEach(t => _presets.Add(t.JobId, t));
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "Error loading JobHud presets.");
            }
        }

        public override void Enable()
        {
            if (!Enabled)
            {
                base.Enable();

                Service.ClientState.Login += OnLogin;
                Service.ClientState.Logout += OnLogout;

                Plugin.SezzUIPlugin.Events.Player.JobChanged += OnJobChanged;
                Plugin.SezzUIPlugin.Events.Player.LevelChanged += OnLevelChanged;
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
                Plugin.SezzUIPlugin.Events.Player.LevelChanged -= OnLevelChanged;
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

            if (!Defaults.JobColors.TryGetValue(jobId, out AccentColor))
                AccentColor = Defaults.IconBarColor;

            if (_presets.TryGetValue(jobId, out BasePreset? preset))
                preset.Configure(this);
        }

        public override void Draw(Vector2 origin)
        {
            if (!Enabled) return;

            // Bars
            if (IsShown || _animator.IsAnimating)
            {
                _animator.Update();

                float yOffset = 0;
                for (int i = 0; i < _bars.Count; i++)
				{
                    Vector2 pos = origin + _positionOffset + _animator.Data.Offset;
                    pos.Y += yOffset;
                    _bars[i].Draw(pos, _animator);
                    yOffset += _bars[i].IconSize.Y + _bars[i].IconPadding;
                }
            }

            // Aura Alerts
            _auraAlerts.ForEach(aa => aa.Draw(origin));
        }

        public void AddBar(Bar bar)
		{
            if (bar.HasIcons)
			{
                _bars.Add(bar);
			}
		}

        public void AddAlert(AuraAlert alert)
		{
            if (alert.Level > 1 && (Service.ClientState.LocalPlayer?.Level ?? 0) < alert.Level) return;

            _auraAlerts.Add(alert);
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

		#region Events
		private void OnLogin(object? sender, EventArgs e)
        {
        }

        private void OnLogout(object? sender, EventArgs e)
        {
            Hide(true);
        }

        private void OnEnteringCombat(object? sender, EventArgs e)
        {
            Show();
        }

        private void OnLeavingCombat(object? sender, EventArgs e)
        {
            Hide();
        }

        private void OnJobChanged(object? sender, GameEvents.JobChangedEventArgs e)
        {
            Configure();
        }

        private void OnLevelChanged(object? sender, GameEvents.LevelChangedEventArgs e)
        {
            Configure();
        }
		#endregion

		public override void Dispose()
        {
            Reset();
            base.Dispose();
        }
    }
}
