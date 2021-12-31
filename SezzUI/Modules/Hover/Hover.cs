using Dalamud.Game.ClientState.Conditions;
using System;
using Dalamud.Logging;
using SezzUI.Interface;
using SezzUI.Interface.GeneralElements;
using SezzUI.Config;
using Dalamud.Game;
using System.Collections.Generic;
using System.Numerics;
using System.Linq;
using ImGuiNET;

namespace SezzUI.Modules.Hover
{
    public class Hover : HudModule
    {
        private HoverConfig Config => (HoverConfig)_config;
        private List<InteractableArea> _areas = new();
        private Dictionary<string, bool> _currentVisibility = new();
        private Dictionary<string, bool> _expectedVisibility = new();

        public override bool Enable()
        {
            if (!base.Enable()) { return false; }

            _initialUpdate = true;

            // https://github.com/shdwp/xivFaderPlugin/blob/5cf28426d24fa7b6866a15549e775dcfd604ac0c/FaderPlugin/Config/Configuration.cs
            using (InteractableArea area = new())
            {
                area.Elements.Add("_MainCommand"); // Main Menu
                area.Elements.Add("_ActionBar03"); // Hotbar 4
                area.Position = new Vector2(4, ImGui.GetMainViewport().Size.Y - 4);
                area.Anchor = Enums.DrawAnchor.BottomLeft;
                area.Size = new Vector2(780, 50); // TODO: Automatic sizing ? Node.Width * Node.ScaleX, Node.Height * Node.ScaleY
                _areas.Add(area);
            };

            using (InteractableArea area = new())
            {
                area.Elements.Add("_ActionBar04"); // Hotbar 5
                area.Elements.Add("_ActionBar09"); // Hotbar 10
                area.Position = new Vector2(ImGui.GetMainViewport().Size.X / 2, ImGui.GetMainViewport().Size.Y - 4);
                area.Anchor = Enums.DrawAnchor.Bottom;
                area.Size = new Vector2(500, 90);
                _areas.Add(area);
            };

            using (InteractableArea area = new())
            {
                area.Elements.Add("_ActionBar06"); // Hotbar 7
                area.Elements.Add("_ActionBar07"); // Hotbar 8
                area.Elements.Add("_ActionBar08"); // Hotbar 9
                area.Position = new Vector2(ImGui.GetMainViewport().Size.X - 4, 710);
                area.Anchor = Enums.DrawAnchor.Right;
                area.Size = new Vector2(176, 670);
                _areas.Add(area);
            };

            using (InteractableArea area = new())
            {
                area.Elements.Add("ScenarioTree"); // Scenario Guide
                //area.Elements.Add("_ToDoList"); // Duty List
                area.Position = new Vector2(ImGui.GetMainViewport().Size.X - 4, 0);
                area.Anchor = Enums.DrawAnchor.TopRight;
                area.Size = new Vector2(340, 670);
                area.Size = new Vector2(340, 100);
                _areas.Add(area);
            };

            Plugin.Framework.Update += FrameworkUpdate;
            return true;
        }

        public override bool Disable()
        {
            if (!base.Disable()) { return false; }
            Plugin.Framework.Update -= FrameworkUpdate;
            UpdateElements(!_hudHidden);
            _areas.Clear();
            _expectedVisibility.Clear();
            _currentVisibility.Clear();
            return true;
        }

        public bool Toggle(bool enable)
        {
            return enable ? Enable() : Disable();
        }

        private void SetAddonVisibility(string name, bool visible)
        {
            try
            {
                var (addons, names) = HudHelper.FindAddonsStartingWith(name);
                for (int i = 0; i < addons.Count; i++)
                {
                    if (names[i] == name)
                    {
                        PluginLog.LogDebug($"[{GetType().Name}] SetAddonVisibility {name} {visible}");
                        HudHelper.SetAddonVisibleTemporary(addons[i], visible, name != "_ToDoList");
                    }
                }
            }
            catch (Exception ex)
            {
                PluginLog.LogError(ex, "Nope!");
            }
        }

        private bool _initialUpdate = true;
        private bool _hudHidden = false;

        public void FrameworkUpdate(Framework framework)
        {
            if (!Plugin.ClientState.IsLoggedIn) { return; }

            bool hudHidden =
                Plugin.Condition[ConditionFlag.WatchingCutscene] ||
                Plugin.Condition[ConditionFlag.WatchingCutscene78] ||
                Plugin.Condition[ConditionFlag.OccupiedInCutSceneEvent] ||
                Plugin.Condition[ConditionFlag.CreatingCharacter] ||
                Plugin.Condition[ConditionFlag.BetweenAreas] ||
                Plugin.Condition[ConditionFlag.BetweenAreas51] ||
                Plugin.Condition[ConditionFlag.OccupiedSummoningBell] ||
                Plugin.Condition[ConditionFlag.OccupiedInQuestEvent] ||
                Plugin.Condition[ConditionFlag.OccupiedInEvent];

            if (!_initialUpdate && _hudHidden == hudHidden) { return; }

            PluginLog.LogDebug($"[{GetType().Name}] hudHidden {hudHidden}");

            _hudHidden = hudHidden;

            foreach (KeyValuePair<string, bool> current in _currentVisibility)
            {
                if (_hudHidden && _expectedVisibility[current.Key] && _currentVisibility[current.Key])
                {
                    SetAddonVisibility(current.Key, false);
                }
                _currentVisibility[current.Key] = !hudHidden;
            }
      
            if (hudHidden)
            {
                UpdateElements(false);
            }
            else
            {
                UpdateElements();
            }

            _initialUpdate = false;
        }

        public void Draw()
        {
            foreach (InteractableArea area in _areas)
            {
                if (area.Enabled)
                {
                    area.Draw();
                    foreach (string addon in area.Elements)
                    {
                        _expectedVisibility[addon] = _hudHidden ? false : area.IsHovered; // TOOD: AtkEvent: MouseOver, MouseOut
                    }
                }
            }

            UpdateElements();
        }

        public void UpdateElements(bool? force = null)
        {
            foreach (KeyValuePair<string, bool> expected in _expectedVisibility)
            {
                if (!_currentVisibility.ContainsKey(expected.Key))
                {
                    _currentVisibility.Add(expected.Key, !expected.Value);
                }

                bool shouldShow = force ?? expected.Value;

                if (_currentVisibility[expected.Key] != shouldShow)
                {
                    if (_hudHidden && shouldShow) { continue; }

                    PluginLog.Debug($"[{this.GetType().Name}] {expected.Key} -> {(shouldShow ? "SHOW" : "HIDE")}");
                    SetAddonVisibility(expected.Key, shouldShow);
                    _currentVisibility[expected.Key] = shouldShow;
                }
            }
        }

        #region Singleton
        private Hover(HoverConfig config) : base(config)
        {
            config.ValueChangeEvent += OnConfigPropertyChanged;
            ConfigurationManager.Instance.ResetEvent += OnConfigReset;

            if (Config.Enabled)
            {
                Enable();
            }
        }

        public static void Initialize() {
            Instance = new Hover(ConfigurationManager.Instance.GetConfigObject<HoverConfig>());
        }

        public static Hover Instance { get; private set; } = null!;

        ~Hover()
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
                    PluginLog.Debug($"[{this.GetType().Name}] OnConfigPropertyChanged Config.Enabled: {Config.Enabled}");
                    Toggle(Config.Enabled);
                    break;
            }
        }

        private void OnConfigReset(ConfigurationManager sender)
        {
            // Configuration doesn't change on reset? 
            PluginLog.Debug($"[{this.GetType().Name}] OnConfigReset");
            _config.ValueChangeEvent -= OnConfigPropertyChanged;
            _config = sender.GetConfigObject<JobHudConfig>();
            _config.ValueChangeEvent += OnConfigPropertyChanged;
            Toggle(Config.Enabled);
            PluginLog.Debug($"[{this.GetType().Name}] Config.Enabled: {Config.Enabled}");
        }
        #endregion
    }
}
