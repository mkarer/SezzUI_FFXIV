using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using SezzUI.Animator;
using SezzUI.Config;
using SezzUI.Enums;
using SezzUI.Helpers;
using SezzUI.Interface;
using SezzUI.Interface.GeneralElements;

namespace SezzUI.Modules.JobHud
{
	public class JobHud : DraggableHudElement, IHudElementWithActor
	{
		private JobHudConfig Config => (JobHudConfig) _config;
#if DEBUG
		private readonly JobHudDebugConfig _debugConfig;
#endif
		public GameObject? Actor { get; set; } = null;
		internal PluginLogger Logger;

		private readonly Animator.Animator _animator = new();
		public bool IsShown { get; private set; }

		private bool _isEnabled;

		internal Vector4 AccentColor;
		public List<Bar> Bars { get; } = new();

		private readonly List<AuraAlert> _auraAlerts = new();
		private readonly Dictionary<uint, BasePreset> _presets = new();

		public int LastDrawTick { get; private set; }

		public int LastDrawElapsed { get; private set; }

		private uint _currentJobId;
		private byte _currentLevel;

		public JobHud(JobHudConfig config, string displayName) : base(config, displayName)
		{
			Logger = new(GetType().Name);
#if DEBUG
			_debugConfig = ConfigurationManager.Instance.GetConfigObject<JobHudDebugConfig>();
#endif
			config.ValueChangeEvent += OnConfigPropertyChanged;
			ConfigurationManager.Instance.Reset += OnConfigReset;

			_animator.Timelines.OnShow.Data.DefaultOpacity = 0;
			_animator.Timelines.OnShow.Data.DefaultOffset.Y = -20;
			_animator.Timelines.OnShow.Add(new FadeAnimation(0, 1, 150));
			_animator.Timelines.OnShow.Add(new TranslationAnimation(_animator.Timelines.OnShow.Data.DefaultOffset, Vector2.Zero, 150));

			_animator.Timelines.OnHide.Data.DefaultOpacity = 1;
			_animator.Timelines.OnHide.Add(new FadeAnimation(1, 0, 150));
			_animator.Timelines.OnHide.Add(new TranslationAnimation(Vector2.Zero, new(0, 20), 150));

			try
			{
				Assembly.GetAssembly(typeof(BasePreset))!.GetTypes().Where(t => t.BaseType == typeof(BasePreset)).Select(t => Activator.CreateInstance(t)).Cast<BasePreset>().ToList().ForEach(t => _presets.Add(t.JobId, t));
			}
			catch (Exception ex)
			{
				Logger.Error(ex, $"Error loading presets: {ex}");
			}

			Toggle(Config.Enabled);
		}

		protected override (List<Vector2>, List<Vector2>) ChildrenPositionsAndSizes() =>
			(new() {Config.Position}, new() {new(300f, 72f)});

		public void Toggle(bool enable = true)
		{
#if DEBUG
			if (_debugConfig.LogGeneral)
			{
				Logger.Debug("Toggle", $"State: {enable}");
			}
#endif

			if (enable && !_isEnabled)
			{
#if DEBUG
				if (_debugConfig.LogGeneral)
				{
					Logger.Debug("Toggle", "Enable");
				}
#endif
				_isEnabled = !_isEnabled;
				Plugin.ClientState.Login += OnLogin;
				Plugin.ClientState.Logout += OnLogout;

				EventManager.Player.JobChanged += OnJobChanged;
				EventManager.Player.LevelChanged += OnLevelChanged;
				EventManager.Combat.EnteringCombat += OnEnteringCombat;
				EventManager.Combat.LeavingCombat += OnLeavingCombat;

				Configure();
			}
			else if (!enable && _isEnabled)
			{
#if DEBUG
				if (_debugConfig.LogGeneral)
				{
					Logger.Debug("Toggle", "Disable");
				}
#endif
				_isEnabled = !_isEnabled;
				Reset();

				Plugin.ClientState.Login -= OnLogin;
				Plugin.ClientState.Logout -= OnLogout;

				EventManager.Player.JobChanged -= OnJobChanged;
				EventManager.Player.LevelChanged -= OnLevelChanged;
				EventManager.Combat.EnteringCombat -= OnEnteringCombat;
				EventManager.Combat.LeavingCombat -= OnLeavingCombat;

				OnLogout(null!, null!);
			}
		}

		private void Reset()
		{
			lock (Bars)
			{
				Hide(true);
				Bars.ForEach(bar => bar.Dispose());
				Bars.Clear();
				_auraAlerts.ForEach(aa => aa.Dispose());
				_auraAlerts.Clear();
				_currentJobId = 0;
				_currentLevel = 0;
			}
		}

		private void Configure()
		{
			lock (Bars)
			{
				PlayerCharacter? player = Plugin.ClientState.LocalPlayer;
				uint jobId = player?.ClassJob.Id ?? 0;
				byte level = player?.Level ?? 0;

				if (_currentLevel == level && _currentJobId == jobId)
				{
					return;
				}

				Reset();

				_currentJobId = jobId;
				_currentLevel = level;

				if (!Defaults.JobColors.TryGetValue(_currentJobId, out AccentColor))
				{
					AccentColor = Defaults.IconBarColor;
				}

				if (_presets.TryGetValue(_currentJobId, out BasePreset? preset))
				{
					preset.Configure(this);
				}

				if (EventManager.Combat.IsInCombat())
				{
					Show();
				}
			}
		}

		public override void DrawChildren(Vector2 origin)
		{
			if (!Config.Enabled || Actor == null || Actor is not PlayerCharacter player || SpellHelper.GetStatus(1534, Unit.Player, false) != null)
			{
				// 1534: Role-playing
				// Condition.RolePlaying ?
				LastDrawTick = 0;
				return;
			}

			// Remember last draw time - if too much time has passed skip hide animations!
			int ticksNow = Environment.TickCount;
			LastDrawElapsed = ticksNow - LastDrawTick;
			LastDrawTick = ticksNow;

			// Bars
			if (IsShown || _animator.IsAnimating)
			{
				_animator.Update();

				Vector2 hudPos = DrawHelper.GetAnchoredPosition(Vector2.Zero, DrawAnchor.Center) + _config.Position + _animator.Data.Offset;
				Vector2 offset = Vector2.Zero;

				for (int i = 0; i < Bars.Count; i++)
				{
					Bar bar = Bars[i];
					if (bar.HasIcons)
					{
						bar.Draw(hudPos + offset, _animator);
						offset.Y += bar.IconSize.Y + bar.IconPadding;
					}
				}
			}

			// Aura Alerts
			_auraAlerts.ForEach(aa => aa.Draw(origin, LastDrawElapsed));
		}

		public void AddBar(Bar bar)
		{
			Bars.Add(bar);
		}

		public void AddAlert(AuraAlert alert)
		{
			if (alert.Level > 1 && (Plugin.ClientState.LocalPlayer?.Level ?? 0) < alert.Level)
			{
				alert.Dispose();
			}
			else
			{
				_auraAlerts.Add(alert);
			}
		}

		public void Show()
		{
			if (!IsShown)
			{
				//Logger.Debug("Show");
				IsShown = !IsShown;
				LastDrawTick = 0;
				_animator.Animate();
			}
		}

		public void Hide(bool force = false)
		{
			if (IsShown)
			{
				//Logger.Debug($"Hide {force}");
				IsShown = !IsShown;
				_animator.Stop(force || LastDrawElapsed > 2000);
			}
		}

		protected override void InternalDispose()
		{
			Toggle(false);
			_presets.Clear();

			ConfigurationManager.Instance.Reset -= OnConfigReset;
			_config.ValueChangeEvent -= OnConfigPropertyChanged;
		}

		#region Configuration Events

		private void OnConfigPropertyChanged(object sender, OnChangeBaseArgs args)
		{
			switch (args.PropertyName)
			{
				case "Enabled":
					Logger.Debug("OnConfigPropertyChanged", $"Config.Enabled: {Config.Enabled}");
					Toggle(Config.Enabled);
					break;
			}
		}

		private void OnConfigReset(ConfigurationManager sender, PluginConfigObject config)
		{
			if (config is not JobHudConfig)
			{
				return;
			}
#if DEBUG
			if (_debugConfig.LogConfigurationManager)
			{
				Logger.Debug("OnConfigReset", "Resetting...");
			}
#endif
			Toggle(false);
#if DEBUG
			if (_debugConfig.LogConfigurationManager)
			{
				Logger.Debug("OnConfigReset", $"Config.Enabled: {Config.Enabled}");
			}
#endif
			Toggle(Config.Enabled);
		}

		#endregion

		#region Game Events

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

		private void OnJobChanged(uint jobId)
		{
			// We're caching current level and job in Configure()
			// to avoid resetting/configuring twice.
			if (_currentJobId != jobId)
			{
				Configure();
			}
		}

		private void OnLevelChanged(byte level)
		{
			// We're caching current level and job in Configure()
			// to avoid resetting/configuring twice.
			if (_currentLevel != level)
			{
				Configure();
			}
		}

		#endregion
	}
}