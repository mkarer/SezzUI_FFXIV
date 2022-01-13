﻿using System;
using System.Linq;
using System.Reflection;
using System.Numerics;
using System.Collections.Generic;
using SezzUI.Config;
using SezzUI.Enums;
using SezzUI.Interface.GeneralElements;
using Dalamud.Game.ClientState.Objects.SubKinds;
using FFXIVClientStructs.FFXIV.Client.Game;
using ImGuiScene;
using ImGuiNET;

namespace SezzUI.Modules.CooldownHud
{
    public class CooldownHud : HudModule
    {
        private CooldownHudConfig Config => (CooldownHudConfig)_config;
        private Dictionary<uint, BasePreset> _presets = new();
        private List<BarManager.BarManager> _barManagers = new();
        private Dictionary<uint, CooldownHudItem> _cooldowns = new();
        private Dictionary<uint, CooldownPulse> _pulses = new();

        private uint _currentJobId = 0;
        private byte _currentLevel = 0;

        private const int MINIMUM_PULSE_INTERVAL = 6000; // Maximum value from config + another 1000ms to be safe.

        private void Reset()
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
            foreach ((_, CooldownPulse pulse) in _pulses)
            {
                pulse.Dispose();
            }
            _pulses.Clear();
        }

        private void Configure()
        {
            Reset();

            PlayerCharacter? player = Plugin.ClientState.LocalPlayer;
            _currentJobId = player?.ClassJob.Id ?? 0;
            _currentLevel = player?.Level ?? 0;

            if (_currentJobId == 0 || _currentLevel == 0) { return; }

            LogDebug("Configure", $"Setting up cooldowns for Job ID: {_currentJobId} Level: {_currentLevel}");

            // Setup watched cooldowns
            if (_presets.TryGetValue(_currentJobId, out BasePreset? preset))
            {
                preset.Configure(this);
            }

            ConfigureBarManagers();
            AddRunningCooldowns();
        }

        private void ConfigureBarManagers()
        {
            // Update BarManager visuals and positioning
        }

        private void AddRunningCooldowns()
        {
            foreach ((uint actionId, _) in _cooldowns)
            {
                GameEvents.CooldownData data = GameEvents.Cooldown.Instance.Get(actionId);
                if (data.IsActive)
                {
                    OnCooldownChanged(actionId, data);
                }
            }
        }

        #region Cooldown Pulse
        private void Pulse(BarManager.BarManagerBar bar) => Pulse(bar.Id, bar.Icon);

        private void Pulse(uint actionId, TextureWrap? texture) {
            if (!_cooldowns.ContainsKey(actionId))
            {
                // This should actually never happen.
                LogError("Pulse", $"Action ID: {actionId} Tried to show cooldown pulse for unknown cooldown!");
                return;
            } 

            LogDebug("Pulse", $"Action ID: {actionId}");

            _cooldowns[actionId].LastPulse = Environment.TickCount64;

            CooldownPulse pulse = new()
            {
                Texture = texture,
                Position = Config.CooldownHudPulse.Position,
                Size = Config.CooldownHudPulse.Size,
                Anchor = Config.CooldownHudPulse.Anchor,
            };
            _pulses[actionId] = pulse;
            pulse.Show();
        }

        private bool ShouldShowPulse(uint actionId) => !_pulses.ContainsKey(actionId) && _cooldowns.ContainsKey(actionId) && Environment.TickCount64 - _cooldowns[actionId].LastPulse > MINIMUM_PULSE_INTERVAL;
        #endregion

        public override void Draw(DrawState state, Vector2? origin)
        {
            if (origin == null || (state != DrawState.Visible && state != DrawState.Partially)) { return; }

            // Bar Managers
            _barManagers.ForEach(barManager =>
            {
                barManager.Draw((Vector2)origin);

                if (Config.CooldownHudPulse.Enabled)
                {
                    foreach (BarManager.BarManagerBar bar in barManager.Bars.Where(bar => bar.IsActive && bar.Remaining <= Math.Abs(Config.CooldownHudPulse.Delay) && ShouldShowPulse(bar.Id)))
                    {
                        Pulse(bar);
                    }
                }
            });

            // Draw pulse animations and remove finished ones
            foreach ((uint actionId, CooldownPulse pulse) in _pulses)
            {
                pulse.Draw((Vector2)origin);
                if (!pulse.Animator.IsAnimating)
                {
                    pulse.Dispose();
                    _pulses.Remove(actionId);
                }
            }
        }

        public void RegisterCooldown(uint actionId, BarManager.BarManager barManager, bool adjustAction = true)
        {
            actionId = adjustAction ? DelvUI.Helpers.SpellHelper.Instance.GetSpellActionId(actionId) : actionId;
            if (_cooldowns.ContainsKey(actionId))
            {
                if (!_cooldowns[actionId].barManagers.Contains(barManager))
                {
                    _cooldowns[actionId].barManagers.Add(barManager);
                    LogDebug("RegisterCooldown", $"Action ID: {actionId} Bar Manager ID: {barManager.Id} ({(string.Join(", ", _cooldowns[actionId].barManagers.Select(x => x.Id)))})");
                }
                else
                {
                    LogError("RegisterCooldown", $"Action ID: {actionId} Failed to register cooldown - already registered to Bar Manager ID: {barManager.Id}");
                }
            }
            else
            {
                CooldownHudItem item = new() { ActionId = actionId };
                item.barManagers.Add(barManager);
                _cooldowns[actionId] = item;
                EventManager.Cooldown.Watch(actionId);
                LogDebug("RegisterCooldown", $"Action ID: {actionId} Bar Manager ID: {barManager.Id}");
            }
        }

        public void RegisterCooldown(uint actionId, string barManagerId, bool adjustAction = true)
        {
            BarManager.BarManager? barManager = _barManagers.Where(x => x.Id == barManagerId).FirstOrDefault();
            if (barManager != null)
            {
                RegisterCooldown(actionId, barManager, adjustAction);
            }
            else
            {
                LogError("RegisterCooldown", $"Action ID: {actionId} Failed to register cooldown - invalid Bar Manager ID: {barManagerId}");
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
                LogError("RegisterCooldown", $"Action ID: {actionId} Failed to register cooldown - invalid Bar Manager Index: {barManagerIndex}");
            }
        }

        public void RegisterCooldown(uint actionId, bool adjustAction)
        {
            RegisterCooldown(actionId, 0, adjustAction);
        }

        private void GetActionDisplayData(uint actionId, ActionType actionType, out string? name, out TextureWrap? texture)
        {
            name = actionType == ActionType.General ?
                Helpers.SpellHelper.Instance.GetGeneralActionName(actionId) :
                Helpers.SpellHelper.Instance.GetActionName(actionId);

            int? iconId = actionType == ActionType.General ?
                Helpers.SpellHelper.Instance.GetGeneralActionIcon(actionId) :
                Helpers.SpellHelper.Instance.GetActionIcon(actionId);

            texture = iconId != null ? DelvUI.Helpers.TexturesCache.Instance.GetTextureFromIconId((uint)iconId) : null;
        }

        public override bool Enable()
        {
            if (!base.Enable()) { return false; }

            EventManager.Player.JobChanged += OnJobChanged;
            EventManager.Player.LevelChanged += OnLevelChanged;

            EventManager.Cooldown.CooldownStarted += OnCooldownStarted;
            EventManager.Cooldown.CooldownChanged += OnCooldownChanged;
            EventManager.Cooldown.CooldownFinished += OnCooldownFinished;

            Configure();

            return true;
        }

        public override bool Disable()
        {
            if (!base.Disable()) { return false; }

            Reset();

            EventManager.Player.JobChanged -= OnJobChanged;
            EventManager.Player.LevelChanged -= OnLevelChanged;

            EventManager.Cooldown.CooldownStarted -= OnCooldownStarted;
            EventManager.Cooldown.CooldownChanged -= OnCooldownChanged;
            EventManager.Cooldown.CooldownFinished -= OnCooldownFinished;

            return true;
        }

        #region Singleton
        public CooldownHud(CooldownHudConfig config) : base(config)
        {
            _config.ValueChangeEvent += OnConfigPropertyChanged;
            ConfigurationManager.Instance.ResetEvent += OnConfigReset;

            // TEMPORARY CONFIGURATION
            BarManager.BarManager primaryBarManager = new("Primary");
            primaryBarManager.Anchor = DrawAnchor.BottomLeft;
            primaryBarManager.Position = new Vector2(22f, ImGui.GetMainViewport().Size.Y - 634f);
            primaryBarManager.BarConfig.Style = BarManagerStyle.Ruri;
            primaryBarManager.BarConfig.FillInverted = true;
            primaryBarManager.BarConfig.ShowDurationRemaining = true;
            _barManagers.Add(primaryBarManager);

            BarManager.BarManager secondaryBarManager = new("Secondary");
            secondaryBarManager.Anchor = DrawAnchor.BottomLeft;
            secondaryBarManager.Position = new Vector2(1649f, 528f);
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
                LogError(ex, $"Error loading presets: {ex}");
            }

            Toggle(Config.Enabled);
        }

        public static CooldownHud Initialize()
        {
            Instance = new CooldownHud(ConfigurationManager.Instance.GetConfigObject<CooldownHudConfig>());
            return Instance;
        }

        public static CooldownHud Instance { get; private set; } = null!;

        protected override void InternalDispose()
        {
            _barManagers.ForEach(manager => manager.Dispose());
            _barManagers.Clear();

            _config.ValueChangeEvent -= OnConfigPropertyChanged;
            ConfigurationManager.Instance.ResetEvent -= OnConfigReset;
        }

        ~CooldownHud()
        {
            Dispose(false);
        }
        #endregion

        #region Configuration Events
        private void OnConfigPropertyChanged(object sender, OnChangeBaseArgs args)
        {
            switch (args.PropertyName)
            {
                case "Enabled":
                    LogDebug("OnConfigPropertyChanged", $"{args.PropertyName}: {Config.Enabled}");
                    Toggle(Config.Enabled);
                    break;

                case "BarManagerRelatedProperty": // TODO: Check sender type for BarManager related settings...
                    if (Enabled)
                    {
                        ConfigureBarManagers();
                    }
                    break;
            }
        }

        private void OnConfigReset(ConfigurationManager sender)
        {
            LogDebug("OnConfigReset", "Resetting...");
            Disable();

            if (_config != null)
            {
                _config.ValueChangeEvent -= OnConfigPropertyChanged;
            }
            _config = sender.GetConfigObject<CooldownHudConfig>();
            _config.ValueChangeEvent += OnConfigPropertyChanged;

            LogDebug("OnConfigReset", $"Config.Enabled: {Config.Enabled}");
            Toggle(Config.Enabled);
        }
        #endregion

        #region Game Events
        private void OnJobChanged(uint jobId)
        {
            // We're caching current level and job in Configure()
            // to avoid resetting/configuring twice.
            if (_currentJobId != jobId) { Configure(); }
        }

        private void OnLevelChanged(byte level)
        {
            // We're caching current level and job in Configure()
            // to avoid resetting/configuring twice.
            if (_currentLevel != level) { Configure(); }
        }

        private void OnCooldownStarted(uint actionId, GameEvents.CooldownData data)
        {
            if (!_cooldowns.ContainsKey(actionId)) { return; }

            _cooldowns[actionId].barManagers.ForEach(barManager =>
            {
                GetActionDisplayData(actionId, data.Type, out string? name, out TextureWrap? texture);
                barManager.Add(actionId, name ?? "Unknown Action", texture, data.StartTime, data.Duration);
                //bool result = barManager.Add(actionId, name ?? "Unknown Action", icon, data.StartTime, data.Duration);
                //LogDebug("OnCooldownStarted", $"BarManager Result: {result} {iconId} Bars: {barManager.Count}");
            });
        }

        private void OnCooldownChanged(uint actionId, GameEvents.CooldownData data)
        {
            if (!_cooldowns.ContainsKey(actionId)) { return; }

            _cooldowns[actionId].barManagers.ForEach(barManager =>
            {
                GetActionDisplayData(actionId, data.Type, out string? name, out TextureWrap? texture);
                barManager.Update(actionId, name ?? "Unknown Action", texture, data.StartTime, data.Duration);
                //bool result = barManager.Update(actionId, name ?? "Unknown Action", icon, data.StartTime, data.Duration);
                //LogDebug("OnCooldownChanged", $"BarManager Result: {result} {iconId} Bars: {barManager.Count}");
            });
        }

        private void OnCooldownFinished(uint actionId, GameEvents.CooldownData data, uint elapsedFinish)
        {
            if (!_cooldowns.ContainsKey(actionId)) { return; }

            if (Config.CooldownHudPulse.Enabled && ShouldShowPulse(actionId))
            {
                // Bar is propably not available anymore here.
                GetActionDisplayData(actionId, data.Type, out string? name, out TextureWrap? texture);
                Pulse(actionId, texture);
            }

            _cooldowns[actionId].barManagers.ForEach(barManager =>
            {
                barManager.Remove(actionId);
                //bool result = barManager.Remove(actionId);
                //LogDebug("OnCooldownFinished", $"BarManager Result: {result} Bars: {barManager.Count}");
            });
        }
        #endregion
    }
}
