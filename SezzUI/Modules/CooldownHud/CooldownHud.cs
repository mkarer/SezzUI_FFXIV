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
        private List<CooldownPulse> _pulses = new();

        private uint _currentJobId = 0;
        private byte _currentLevel = 0;

        private const ushort INITIAL_PULSE_CHARGES = 100; // Unreachable amount of charges.
        private const ushort NOPULSE_AFTER_ELAPSEDFINISHED = 3000; // Don't show pulse if the cooldown finished ages ago...

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
            _pulses.ForEach(pulse => pulse.Dispose());
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
                    OnCooldownChanged(actionId, data, false);
                }
            }
        }

        #region Cooldown Pulse
        private void Pulse(BarManager.BarManagerBar bar, bool early) => Pulse(bar.Id, bar.Icon, (ushort)((bar.Data != null ? (ushort)bar.Data : 0) + (early ? 1 : 0)));

        private void Pulse(uint actionId, TextureWrap? texture, ushort charges)
        {
            if (!_cooldowns.ContainsKey(actionId))
            {
                // This should actually never happen.
                LogError("Pulse", $"Action ID: {actionId} Tried to show cooldown pulse for unknown cooldown!");
                return;
            }

            LogDebug("Pulse", $"Action ID: {actionId} Charges: {charges}");

            _cooldowns[actionId].LastPulseCharges = charges;

            CooldownPulse pulse = new()
            {
                ActionId = actionId,
                Charges = charges,
                Texture = texture,
                Position = Config.CooldownHudPulse.Position,
                Size = Config.CooldownHudPulse.Size,
                Anchor = Config.CooldownHudPulse.Anchor,
            };
            _pulses.Add(pulse);
            pulse.Show();
        }

        private bool CanPulse(uint actionId, ushort charges)
        {
            return Config.CooldownHudPulse.Enabled && charges > 0 &&
                _pulses.Count(pulse => pulse.ActionId == actionId && pulse.Charges == charges) == 0 && // Not currently showing animations for this cooldown at this charges
                _cooldowns.ContainsKey(actionId) && // Cooldown is watched
                _cooldowns[actionId].LastPulseCharges != charges; // Last shown pulse for this action was for another amount of charges
        }
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
                    foreach (BarManager.BarManagerBar bar in barManager.Bars.Where(bar => bar.IsActive && bar.Remaining <= Math.Abs(Config.CooldownHudPulse.Delay) && CanPulse(bar.Id, bar.Data != null ? (ushort)((ushort)bar.Data + 1) : (ushort)0)))
                    {
                        Pulse(bar, true);
                    }
                }
            });

            // Update pulse animations and remove finished ones
            if (_pulses.Any())
            {
                for (int i = _pulses.Count - 1; i >= 0; i--)
                {
                    CooldownPulse pulse = _pulses[i];
                    bool expired = Environment.TickCount64 - pulse.Created >= NOPULSE_AFTER_ELAPSEDFINISHED;
                    if (!expired || pulse.Animator.IsAnimating)
                    {
                        pulse.Draw((Vector2)origin);
                    }
                    if (expired && !pulse.Animator.IsAnimating)
                    {
                        LogDebug("Draw", $"Removing CooldownPulse: Action ID: {pulse.ActionId} Charges: {pulse.Charges} Created: {pulse.Created} Expired: {expired} Animating: {pulse.Animator.IsAnimating}"); ;
                        pulse.Dispose();
                        _pulses.RemoveAt(i);
                    }
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
                CooldownHudItem item = new()
                {
                    ActionId = actionId,
                    LastPulseCharges = INITIAL_PULSE_CHARGES,
                };
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

            _cooldowns[actionId].LastPulseCharges = INITIAL_PULSE_CHARGES;
            _cooldowns[actionId].barManagers.ForEach(barManager =>
            {
                GetActionDisplayData(actionId, data.Type, out string? name, out TextureWrap? texture);
                barManager.Add(actionId, name ?? "Unknown Action", data.MaxCharges > 1 && data.CurrentCharges > 0 ? "x1" : null, texture, data.StartTime, data.Duration, data.CurrentCharges);
                //bool result = barManager.Add(actionId, name ?? "Unknown Action", icon, data.StartTime, data.Duration);
                //LogDebug("OnCooldownStarted", $"BarManager Result: {result} {iconId} Bars: {barManager.Count}");
            });
        }

        private void OnCooldownChanged(uint actionId, GameEvents.CooldownData data, bool chargesChanged, ushort previousCharges = 0)
        {
            if (!_cooldowns.ContainsKey(actionId)) { return; }

            _cooldowns[actionId].barManagers.ForEach(barManager =>
            {
                GetActionDisplayData(actionId, data.Type, out string? name, out TextureWrap? texture);

                barManager.Update(actionId, name ?? "Unknown Action", data.MaxCharges > 1 && data.CurrentCharges > 0 ? "x1" : null, texture, data.StartTime, data.Duration, data.CurrentCharges);
                //bool result = barManager.Update(actionId, name ?? "Unknown Action", icon, data.StartTime, data.Duration);
                //LogDebug("OnCooldownChanged", $"BarManager Result: {result} {iconId} Bars: {barManager.Count}");

                // Pulse if charges changed and it wasn't already triggered by the customized delay in Draw()
                if (chargesChanged)
                {
                    _cooldowns[actionId].LastPulseCharges = INITIAL_PULSE_CHARGES;
                }
                if (chargesChanged && data.CurrentCharges > 0 && CanPulse(actionId, data.CurrentCharges))
                {
                    BarManager.BarManagerBar? bar = barManager.Get(actionId);
                    if (bar != null)
                    {
                        Pulse(bar, false);
                    }
                }
            });
        }

        private void OnCooldownFinished(uint actionId, GameEvents.CooldownData data, uint elapsedFinish)
        {
            if (!_cooldowns.ContainsKey(actionId)) { return; }

            if (elapsedFinish <= NOPULSE_AFTER_ELAPSEDFINISHED && CanPulse(actionId, data.CurrentCharges))
            {
                // Bar is very likely not available anymore here, because it was removed by BarManager.RemoveExpired
                GetActionDisplayData(actionId, data.Type, out string? name, out TextureWrap? texture);
                Pulse(actionId, texture, data.CurrentCharges);
            }

            _cooldowns[actionId].LastPulseCharges = INITIAL_PULSE_CHARGES;
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
