using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Dalamud.Game.ClientState.Objects.SubKinds;
using FFXIVClientStructs.FFXIV.Client.Game;
using ImGuiScene;
using SezzUI.Configuration;
using SezzUI.Enums;
using SezzUI.Game.Events;
using SezzUI.Game.Events.Cooldown;
using SezzUI.Helper;
using SezzUI.Interface.BarManager;
using SezzUI.Modules.CooldownHud.Jobs;

namespace SezzUI.Modules.CooldownHud
{
	public class CooldownHud : PluginModule
	{
		private const ushort INITIAL_PULSE_CHARGES = 100; // Unreachable amount of charges.
		private const ushort NO_PULSE_AFTER_ELAPSED_FINISHED = 3000; // Don't show pulse if the cooldown finished ages ago...
		private readonly List<BarManager> _barManagers = new();
		private readonly Dictionary<uint, CooldownHudItem> _cooldowns = new();
		private readonly Dictionary<uint, BasePreset> _presets = new();
		private readonly Dictionary<uint, int?> _iconOverride = new();
		private readonly List<CooldownPulse> _pulses = new();

		private uint _currentJobId;
		private byte _currentLevel;
#if DEBUG
		private readonly CooldownHudDebugConfig _debugConfig;
#endif
		private CooldownHudConfig Config => (CooldownHudConfig) _config;

		private void Reset()
		{
			lock (_cooldowns)
			{
				_currentJobId = 0;
				_currentLevel = 0;

				// Reset BarManagers
				_barManagers.ForEach(manager => manager.Clear());

				// Reset watched cooldowns
				foreach ((uint actionId, CooldownHudItem item) in _cooldowns)
				{
					EventManager.Cooldown.Unwatch(actionId);
					item.Dispose();
				}

				_cooldowns.Clear();

				// Remove all pulse animations
				_pulses.ForEach(pulse => pulse.Dispose());
				_pulses.Clear();
			}
		}

		private void Configure()
		{
			lock (_cooldowns)
			{
				PlayerCharacter? player = Service.ClientState.LocalPlayer;
				uint jobId = player?.ClassJob.Id ?? 0;
				byte level = player?.Level ?? 0;

				if (_currentLevel == level && _currentJobId == jobId)
				{
					return;
				}

				Reset();

				_currentJobId = jobId;
				_currentLevel = level;

				if (_currentJobId == 0 || _currentLevel == 0)
				{
					return;
				}

#if DEBUG
				if (_debugConfig.LogGeneral)
				{
					Logger.Debug($"Setting up cooldowns for Job ID: {_currentJobId} Level: {_currentLevel}");
				}
#endif

				// Setup watched cooldowns
				if (_presets.TryGetValue(_currentJobId, out BasePreset? preset))
				{
					preset.Configure(this);
				}
				else
				{
					_presets[0].Configure(this);
				}

				ConfigureBarManagers();
				AddRunningCooldowns();
			}
		}

		private void ConfigureBarManagers()
		{
			// Update BarManager visuals and positioning
		}

		private void AddRunningCooldowns()
		{
			foreach ((uint actionId, _) in _cooldowns)
			{
				CooldownData data = EventManager.Cooldown.Get(actionId);
				if (data.IsActive)
				{
					OnCooldownChanged(actionId, data, false);
				}
			}
		}

		protected override void OnDraw(DrawState state)
		{
			if (state != DrawState.Visible && state != DrawState.Partially)
			{
				return;
			}

			// Bar Managers
			_barManagers.ForEach(barManager =>
			{
				barManager.Draw();
				if (Config.CooldownHudPulse.Enabled)
				{
					foreach (BarManagerBar bar in barManager.Bars.Where(bar => bar.IsActive && bar.Remaining <= Math.Abs(Config.CooldownHudPulse.Delay) && CanPulse(bar.Id, bar.Data != null ? (ushort) ((ushort) bar.Data + 1) : (ushort) 0)))
					{
						Pulse(bar, true);
					}
				}
			});

			// Update pulse animations and remove finished ones
			for (int i = _pulses.Count - 1; i >= 0; i--)
			{
				CooldownPulse pulse = _pulses[i];
				bool expired = Environment.TickCount64 - pulse.Created >= NO_PULSE_AFTER_ELAPSED_FINISHED;
				if (!expired && pulse.Animator.IsAnimating)
				{
					pulse.Draw();
					continue;
				}
#if DEBUG
				if (_debugConfig.LogCooldownPulseAnimations)
				{
					Logger.Debug($"Removing CooldownPulse: Action ID: {pulse.ActionId} Charges: {pulse.Charges} Created: {pulse.Created} Expired: {expired} Animating: {pulse.Animator.IsAnimating}");
				}
#endif
				pulse.Dispose();
				_pulses.RemoveAt(i);
			}
		}

		public void RegisterCooldown(uint actionId, BarManager barManager, bool adjustAction = true)
		{
			switch (actionId)
			{
				case 3:
					// Sprint Spell Icon != Sprint General Action Icon
					if (!_iconOverride.ContainsKey(actionId))
					{
						_iconOverride[actionId] = SpellHelper.GetGeneralActionIconId(4);
					}

					break;
			}

			actionId = adjustAction ? SpellHelper.GetAdjustedActionId(actionId) : actionId;
			if (_cooldowns.ContainsKey(actionId))
			{
				if (!_cooldowns[actionId].BarManagers.Contains(barManager))
				{
					_cooldowns[actionId].BarManagers.Add(barManager);
#if DEBUG
					if (_debugConfig.LogCooldownRegistration)
					{
						Logger.Debug($"Action ID: {actionId} Bar Manager ID: {barManager.Id} ({string.Join(", ", _cooldowns[actionId].BarManagers.Select(x => x.Id))})");
					}
#endif
				}
				else
				{
					Logger.Error($"Action ID: {actionId} Failed to register cooldown - already registered to Bar Manager ID: {barManager.Id}");
				}
			}
			else
			{
				CooldownHudItem item = new()
				{
					ActionId = actionId,
					LastPulseCharges = INITIAL_PULSE_CHARGES
				};
				item.BarManagers.Add(barManager);
				_cooldowns[actionId] = item;
				EventManager.Cooldown.Watch(actionId);
#if DEBUG
				if (_debugConfig.LogCooldownRegistration)
				{
					Logger.Debug($"Action ID: {actionId} Bar Manager ID: {barManager.Id}");
				}
#endif
			}
		}

		public void RegisterCooldown(uint actionId, string barManagerId, bool adjustAction = true)
		{
			BarManager? barManager = _barManagers.Where(x => x.Id == barManagerId).FirstOrDefault();
			if (barManager != null)
			{
				RegisterCooldown(actionId, barManager, adjustAction);
			}
			else
			{
				Logger.Error($"Action ID: {actionId} Failed to register cooldown - invalid Bar Manager ID: {barManagerId}");
			}
		}

		public void RegisterCooldown(uint actionId, int barManagerIndex = 0, bool adjustAction = true)
		{
			if (_barManagers.Count > barManagerIndex)
			{
				RegisterCooldown(actionId, _barManagers[barManagerIndex], adjustAction);
			}
			else
			{
				Logger.Error($"Action ID: {actionId} Failed to register cooldown - invalid Bar Manager Index: {barManagerIndex}");
			}
		}

		public void RegisterCooldown(uint actionId, bool adjustAction)
		{
			RegisterCooldown(actionId, 0, adjustAction);
		}

		private void GetActionDisplayData(uint actionId, ActionType actionType, out string? name, out TextureWrap? texture)
		{
			name = actionType == ActionType.General ? SpellHelper.GetGeneralActionName(actionId) : SpellHelper.GetActionName(actionId);
			if (!_iconOverride.TryGetValue(actionId, out int? iconId))
			{
				iconId = actionType == ActionType.General ? SpellHelper.GetGeneralActionIconId(actionId) : SpellHelper.GetActionIconId(actionId);
			}

			texture = SpellHelper.GetIconTexture((ushort?) iconId, out bool _);
		}

		protected override void OnEnable()
		{
			EventManager.Player.JobChanged += OnJobChanged;
			EventManager.Player.LevelChanged += OnLevelChanged;
			EventManager.Cooldown.CooldownStarted += OnCooldownStarted;
			EventManager.Cooldown.CooldownChanged += OnCooldownChanged;
			EventManager.Cooldown.CooldownFinished += OnCooldownFinished;
			Singletons.Get<MediaManager>().PathChanged += OnMediaPathChanged;

			Configure();
		}

		protected override void OnDisable()
		{
			Reset();

			EventManager.Player.JobChanged -= OnJobChanged;
			EventManager.Player.LevelChanged -= OnLevelChanged;

			EventManager.Cooldown.CooldownStarted -= OnCooldownStarted;
			EventManager.Cooldown.CooldownChanged -= OnCooldownChanged;
			EventManager.Cooldown.CooldownFinished -= OnCooldownFinished;

			Singletons.Get<MediaManager>().PathChanged -= OnMediaPathChanged;
		}

		#region Cooldown Pulse

		private void Pulse(BarManagerBar bar, bool early)
		{
			Pulse(bar.Id, bar.Icon, (ushort) ((bar.Data != null ? (ushort) bar.Data : 0) + (early ? 1 : 0)));
		}

		private void Pulse(uint actionId, TextureWrap? texture, ushort charges)
		{
			if (!_cooldowns.ContainsKey(actionId))
			{
				// This should actually never happen.
				Logger.Error($"Action ID: {actionId} Tried to show cooldown pulse for unknown cooldown!");
				return;
			}
#if DEBUG
			if (_debugConfig.LogCooldownPulseAnimations)
			{
				Logger.Debug($"Action ID: {actionId} Charges: {charges}");
			}
#endif
			_cooldowns[actionId].LastPulseCharges = charges;

			CooldownPulse pulse = new()
			{
				ActionId = actionId,
				Charges = charges,
				Texture = texture,
				Position = Config.CooldownHudPulse.Position,
				Size = Config.CooldownHudPulse.Size,
				Anchor = Config.CooldownHudPulse.Anchor
			};
			_pulses.Add(pulse);
			pulse.Show();
		}

		private bool CanPulse(uint actionId, ushort charges)
		{
			return Config.CooldownHudPulse.Enabled && charges > 0 && _pulses.Count(pulse => pulse.ActionId == actionId && pulse.Charges == charges) == 0 && // Not currently showing animations for this cooldown at this charges
					_cooldowns.ContainsKey(actionId) && // Cooldown is watched
					_cooldowns[actionId].LastPulseCharges != charges; // Last shown pulse for this action was for another amount of charges
		}

		#endregion

		public CooldownHud(CooldownHudConfig config) : base(config)
		{
			_config.ValueChangeEvent += OnConfigPropertyChanged;
			Singletons.Get<ConfigurationManager>().Reset += OnConfigReset;
#if DEBUG
			_debugConfig = Singletons.Get<ConfigurationManager>().GetConfigObject<CooldownHudDebugConfig>();
#endif

			// TEMPORARY CONFIGURATION
			BarManager primaryBarManager = new("Primary");
			primaryBarManager.Anchor = DrawAnchor.BottomLeft;
			primaryBarManager.Position = new(22f, -634f);
			primaryBarManager.BarConfig.Style = BarManagerStyle.Ruri;
			primaryBarManager.BarConfig.FillInverted = true;
			primaryBarManager.BarConfig.ShowDurationRemaining = true;
			_barManagers.Add(primaryBarManager);

			BarManager secondaryBarManager = new("Secondary");
			secondaryBarManager.Anchor = DrawAnchor.Center;
			secondaryBarManager.Position = new(369, 0);
			secondaryBarManager.BarConfig.Style = BarManagerStyle.Ruri;
			secondaryBarManager.BarConfig.FillInverted = true;
			secondaryBarManager.BarConfig.ShowDurationRemaining = true;
			_barManagers.Add(secondaryBarManager);

			try
			{
				Assembly.GetAssembly(typeof(BasePreset))!.GetTypes().Where(t => t.BaseType == typeof(BasePreset)).Select(t => Activator.CreateInstance(t)).Cast<BasePreset>().ToList().ForEach(t => _presets.Add(t.JobId, t));
			}
			catch (Exception ex)
			{
				Logger.Error($"Error loading presets: {ex}");
			}
			
			_presets.Keys.ToList().ForEach(jobId =>
			{
				BasePreset preset = _presets[jobId];
				uint parentJobId = JobsHelper.GetParentJobId(jobId);
				if (preset.JobId != JobIDs.SCH && parentJobId != 0 && parentJobId != preset.JobId && !_presets.ContainsKey(parentJobId))
				{
					_presets.Add(parentJobId, preset);
				}
			});

			(this as IPluginComponent).SetEnabledState(Config.Enabled);
		}

		protected override void OnDispose()
		{
			_config.ValueChangeEvent -= OnConfigPropertyChanged;
			Singletons.Get<ConfigurationManager>().Reset -= OnConfigReset;

			_barManagers.ForEach(manager => manager.Dispose());
			_barManagers.Clear();
			_presets.Clear();
			_iconOverride.Clear();
		}

		#region Configuration Events

		private void OnConfigPropertyChanged(object sender, OnChangeBaseArgs args)
		{
			switch (args.PropertyName)
			{
				case "Enabled":
#if DEBUG
					if (_debugConfig.LogConfigurationManager)
					{
						Logger.Debug($"{args.PropertyName}: {Config.Enabled}");
					}
#endif
					(this as IPluginComponent).SetEnabledState(Config.Enabled);
					break;

				case "BarManagerRelatedProperty": // TODO: Check sender type for BarManager related settings...
					if ((this as IPluginComponent).IsEnabled)
					{
						ConfigureBarManagers();
					}

					break;
			}
		}

		private void OnConfigReset(ConfigurationManager sender, PluginConfigObject config)
		{
			if (config != _config)
			{
				return;
			}

#if DEBUG
			if (_debugConfig.LogConfigurationManager)
			{
				Logger.Debug("Resetting...");
			}
#endif
			(this as IPluginComponent).Disable();
#if DEBUG
			if (_debugConfig.LogConfigurationManager)
			{
				Logger.Debug($"Config.Enabled: {Config.Enabled}");
			}
#endif
			(this as IPluginComponent).SetEnabledState(Config.Enabled);
		}

		#endregion

		private void OnMediaPathChanged(string path) => (this as IPluginComponent).Reload();

		#region Game Events

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

		private void OnCooldownStarted(uint actionId, CooldownData data)
		{
			if (!_cooldowns.ContainsKey(actionId))
			{
				return;
			}

			_cooldowns[actionId].LastPulseCharges = INITIAL_PULSE_CHARGES;
			_cooldowns[actionId].BarManagers.ForEach(barManager =>
			{
				GetActionDisplayData(actionId, data.Type, out string? name, out TextureWrap? texture);
				bool result = barManager.Add(actionId, name ?? "Unknown Action", data.MaxCharges > 1 && data.CurrentCharges > 0 ? "x1" : null, texture, data.StartTime, data.Duration, data.CurrentCharges);
#if DEBUG
				if (_debugConfig.LogCooldownEventHandling)
				{
					Logger.Debug($"BarManager Result: {result} Active Bars: {barManager.Count}");
				}
#endif
			});
		}

		private void OnCooldownChanged(uint actionId, CooldownData data, bool chargesChanged, ushort previousCharges = 0)
		{
			if (!_cooldowns.ContainsKey(actionId))
			{
				return;
			}

			_cooldowns[actionId].BarManagers.ForEach(barManager =>
			{
				GetActionDisplayData(actionId, data.Type, out string? name, out TextureWrap? texture);
				bool result = barManager.Update(actionId, name ?? "Unknown Action", data.MaxCharges > 1 && data.CurrentCharges > 0 ? "x1" : null, texture, data.StartTime, data.Duration, data.CurrentCharges);
#if DEBUG
				if (_debugConfig.LogCooldownEventHandling)
				{
					Logger.Debug($"BarManager Result: {result} Active Bars: {barManager.Count}");
				}
#endif

				// Pulse if charges changed and it wasn't already triggered by the customized delay in Draw()
				if (chargesChanged)
				{
					_cooldowns[actionId].LastPulseCharges = INITIAL_PULSE_CHARGES;
				}

				if (chargesChanged && data.CurrentCharges > 0 && CanPulse(actionId, data.CurrentCharges))
				{
					BarManagerBar? bar = barManager.Get(actionId);
					if (bar != null)
					{
						Pulse(bar, false);
					}
				}
			});
		}

		private void OnCooldownFinished(uint actionId, CooldownData data, uint elapsedFinish)
		{
			if (!_cooldowns.ContainsKey(actionId))
			{
				return;
			}

			if (elapsedFinish <= NO_PULSE_AFTER_ELAPSED_FINISHED && CanPulse(actionId, data.CurrentCharges))
			{
				// Bar is very likely not available anymore here, because it was removed by BarManager.RemoveExpired
				GetActionDisplayData(actionId, data.Type, out string? _, out TextureWrap? texture);
				Pulse(actionId, texture, data.CurrentCharges);
			}

			_cooldowns[actionId].LastPulseCharges = INITIAL_PULSE_CHARGES;
			_cooldowns[actionId].BarManagers.ForEach(barManager =>
			{
				bool result = barManager.Remove(actionId);
#if DEBUG
				if (_debugConfig.LogCooldownEventHandling)
				{
					Logger.Debug($"BarManager Result: {result} Active Bars: {barManager.Count}");
				}
#endif
			});
		}

		#endregion
	}
}