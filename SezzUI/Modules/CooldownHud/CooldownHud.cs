using System;
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

        private uint _currentJobId = 0;
        private byte _currentLevel = 0;

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

            // Create bars for already running cooldowns
            AddRunningCooldowns();
        }

        private void ConfigureBarManagers()
        {
            // Update BarManager visuals and positioning
        }

        private void AddRunningCooldowns()
        {
            foreach (var kvp in _cooldowns)
            {
                // Check if action is on cooldown using CooldownManager
            }
        }

        public override void Draw(DrawState state, Vector2? origin)
        {
            if (origin == null || (state != DrawState.Visible && state != DrawState.Partially)) { return; }

            _barManagers.ForEach(barManager =>
            {
                barManager.Draw((Vector2)origin);
            });
        }

        public void RegisterCooldown(uint actionId, BarManager.BarManager barManager)
        {
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
                CooldownHudItem item = new() { actionId = actionId };
                item.barManagers.Add(barManager);
                _cooldowns[actionId] = item;
                EventManager.Cooldown.Watch(actionId);
                LogDebug("RegisterCooldown", $"Action ID: {actionId} Bar Manager ID: {barManager.Id}");
            }
        }

        public void RegisterCooldown(uint actionId, string barManagerId)
        {
            BarManager.BarManager? barManager = _barManagers.Where(x => x.Id == barManagerId).FirstOrDefault();
            if (barManager != null)
            {
                RegisterCooldown(actionId, barManager);
            }
            else
            {
                LogError("RegisterCooldown", $"Action ID: {actionId} Failed to register cooldown - invalid Bar Manager ID: {barManagerId}");
            }
        }

        public void RegisterCooldown(uint actionId, int barManagerIndex = 0)
        {
            if (_barManagers.Count > barManagerIndex)
            {
                RegisterCooldown(actionId, _barManagers[barManagerIndex]);
            }
            else
            {
                LogError("RegisterCooldown", $"Action ID: {actionId} Failed to register cooldown - invalid Bar Manager Index: {barManagerIndex}");
            }
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
            ConfigureBarManagers();

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
                string? name = data.Type == ActionType.General ?
                    Helpers.SpellHelper.Instance.GetGeneralActionName(actionId) :
                    Helpers.SpellHelper.Instance.GetActionName(actionId);

                int? iconId = data.Type == ActionType.General ?
                    Helpers.SpellHelper.Instance.GetGeneralActionIcon(actionId) :
                    Helpers.SpellHelper.Instance.GetActionIcon(actionId);

                TextureWrap? icon = iconId != null ? DelvUI.Helpers.TexturesCache.Instance.GetTextureFromIconId((uint)iconId) : null;

                bool result = barManager.Add(actionId, name ?? "Unknown Action", icon, data.StartTime, data.Duration);
                LogDebug("OnCooldownStarted", $"BarManager Result: {result} {iconId} Bars: {barManager.Count}");
            });
        }

        private void OnCooldownChanged(uint actionId, GameEvents.CooldownData data)
        {
            if (!_cooldowns.ContainsKey(actionId)) { return; }

            _cooldowns[actionId].barManagers.ForEach(barManager =>
            {
                string? name = data.Type == ActionType.General ?
                    Helpers.SpellHelper.Instance.GetGeneralActionName(actionId) :
                    Helpers.SpellHelper.Instance.GetActionName(actionId);

                int? iconId = data.Type == ActionType.General ?
                    Helpers.SpellHelper.Instance.GetGeneralActionIcon(actionId) :
                    Helpers.SpellHelper.Instance.GetActionIcon(actionId);

                TextureWrap? icon = iconId != null ? DelvUI.Helpers.TexturesCache.Instance.GetTextureFromIconId((uint)iconId) : null;

                bool result = barManager.Update(actionId, name ?? "Unknown Action", icon, data.StartTime, data.Duration);
                LogDebug("OnCooldownChanged", $"BarManager Result: {result} {iconId} Bars: {barManager.Count}");
            });
        }

        private void OnCooldownFinished(uint actionId, GameEvents.CooldownData data, uint elapsedFinish)
        {
            if (!_cooldowns.ContainsKey(actionId)) { return; }

            _cooldowns[actionId].barManagers.ForEach(barManager =>
            {
                bool result = barManager.Remove(actionId);
                LogDebug("OnCooldownFinished", $"BarManager Result: {result} Bars: {barManager.Count}");
            });
        }
        #endregion
    }
}
