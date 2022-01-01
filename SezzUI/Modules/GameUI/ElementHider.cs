using Dalamud.Game.ClientState.Conditions;
using System;
using Dalamud.Logging;
using SezzUI.Interface.GeneralElements;
using SezzUI.Config;
using Dalamud.Game;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using ImGuiNET;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace SezzUI.Modules.GameUI
{
    public class ElementHider : HudModule
    {
        private ElementHiderConfig Config => (ElementHiderConfig)_config;
        private List<InteractableArea> _areas = new();

        private bool _initialUpdate = true;
        private bool _hudHidden = false;

        /// <summary>
        /// Contains the current (assumed) visiblity state of default game elements.
        /// </summary>
        private Dictionary<Element, bool> _currentVisibility = new();

        /// <summary>
        /// Contains the expected visiblity state of default game elements based on mouseover state or other events.
        /// </summary>
        private Dictionary<Element, bool> _expectedVisibility = new();

        public override bool Enable()
        {
            if (!base.Enable()) { return false; }

            _initialUpdate = true;

            // TEMPORARY CONFIGURATION
            // TODO: Add to configuration UI with placeholders
            using (InteractableArea area = new(new InteractableAreaConfig()))
            {
                area.Elements.AddRange(new List<Element> { Element.MainMenu, Element.ActionBar04 });
                area.Position = new Vector2(4, ImGui.GetMainViewport().Size.Y - 4);
                area.Anchor = Enums.DrawAnchor.BottomLeft;
                area.Size = new Vector2(780, 50); // TODO: Automatic sizing ? Node.Width * Node.ScaleX, Node.Height * Node.ScaleY
                _areas.Add(area);
            };

            using (InteractableArea area = new(new InteractableAreaConfig()))
            {
                area.Elements.AddRange(new List<Element> { Element.ActionBar05, Element.ActionBar10 });
                area.Position = new Vector2(ImGui.GetMainViewport().Size.X / 2, ImGui.GetMainViewport().Size.Y - 4);
                area.Anchor = Enums.DrawAnchor.Bottom;
                area.Size = new Vector2(500, 90);
                _areas.Add(area);
            };

            using (InteractableArea area = new(new InteractableAreaConfig()))
            {
                area.Elements.AddRange(new List<Element> { Element.ActionBar07, Element.ActionBar08, Element.ActionBar09 });
                area.Position = new Vector2(ImGui.GetMainViewport().Size.X - 4, 710);
                area.Anchor = Enums.DrawAnchor.Right;
                area.Size = new Vector2(176, 670);
                _areas.Add(area);
            };

            using (InteractableArea area = new(new InteractableAreaConfig()))
            {
                area.Elements.AddRange(new List<Element> { Element.ScenarioGuide });
                area.Position = new Vector2(ImGui.GetMainViewport().Size.X - 4, 0);
                area.Anchor = Enums.DrawAnchor.TopRight;
                area.Size = new Vector2(340, 670);
                area.Size = new Vector2(340, 100);
                _areas.Add(area);
            };

            if (Config.HideActionBarLock) { _expectedVisibility[Element.ActionBarLock] = false; }

            Plugin.Framework.Update += FrameworkUpdate;
            return true;
        }

        public override bool Disable()
        {
            if (!base.Disable()) { return false; }

            Plugin.Framework.Update -= FrameworkUpdate;
            UpdateAddons(_expectedVisibility, !_hudHidden);
            _areas.Clear();
            _expectedVisibility.Clear();
            _currentVisibility.Clear();

            return true;
        }

        public bool Toggle(bool enable)
        {
            return enable ? Enable() : Disable();
        }

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

            _hudHidden = hudHidden;
            PluginLog.LogDebug($"[{GetType().Name}] Game UI visibility: {(hudHidden ? "HIDDEN" : "VISIBLE")}");

            if (_initialUpdate)
            {
                // Initial update, expect incorrect visibility on all addons to force update
                PluginLog.LogDebug($"[{GetType().Name}] Initial update...");
                foreach (KeyValuePair<Element, bool> expected in _expectedVisibility)
                {
                    _currentVisibility[expected.Key] = !expected.Value;
                }
            }

            if (hudHidden)
            {
                // Hide all, ignore expected states
                UpdateAddons(_expectedVisibility, false);
            }
            else
            {
                // Toggle visibility based on state
                Dictionary<Element, bool>? update = null;

                foreach (KeyValuePair<Element, bool> expected in _expectedVisibility)
                {
                    if (expected.Value != _currentVisibility[expected.Key])
                    {
                        if (update == null) { update = new(); }
                        update[expected.Key] = expected.Value;
                    }
                }

                if (update != null) { UpdateAddons(update);  }
            }

            _initialUpdate = false;
        }

        public void Draw()
        {
            if (!Enabled) { return; }

            bool updateNeeded = false;

            foreach (InteractableArea area in _areas)
            {
                if (area.Enabled)
                {
                    area.Draw();
                    foreach (Element element in area.Elements)
                    {
                        _expectedVisibility[element] = _hudHidden ? false : area.IsHovered; // TOOD: AtkEvent: MouseOver, MouseOut
                        updateNeeded = updateNeeded || (!_currentVisibility.ContainsKey(element) || _currentVisibility[element] != _expectedVisibility[element]);
                    }
                }
            }

            if (updateNeeded) { UpdateAddons(_expectedVisibility); }
        }

        #region Addons
        private static Dictionary<Element, string> _elementAddonNames = new()
        {
            { Element.ActionBar01, "_ActionBar" },
            { Element.ActionBar02, "_ActionBar01" },
            { Element.ActionBar03, "_ActionBar02" },
            { Element.ActionBar04, "_ActionBar03" },
            { Element.ActionBar05, "_ActionBar04" },
            { Element.ActionBar06, "_ActionBar05" },
            { Element.ActionBar07, "_ActionBar06" },
            { Element.ActionBar08, "_ActionBar07" },
            { Element.ActionBar09, "_ActionBar08" },
            { Element.ActionBar10, "_ActionBar09" },
            { Element.CastBar, "_CastBar" },
            { Element.ExperienceBar, "_Exp" },
            { Element.InventoryGrid, "_BagWidget" },
            { Element.Currency, "_Money" },
            { Element.ScenarioGuide, "ScenarioTree" },
            { Element.QuestLog, "_ToDoList" },
            { Element.MainMenu, "_MainCommand" },
            { Element.Minimap, "_NaviMap" },
            { Element.PartyList, "_PartyList" },
            { Element.LimitBreak, "_LimitBreak" },
            { Element.Parameters, "_ParameterWidget" },
            { Element.Status, "_Status" },
            { Element.StatusEnhancements, "_StatusCustom0" },
            { Element.StatusEnfeeblements, "_StatusCustom1" },
            { Element.StatusOther, "_StatusCustom2" },
        };

        private unsafe void UpdateAddonVisibility(Element element, AtkUnitBase* addon, bool shouldShow, bool modifyNodeList = true)
        {
            _currentVisibility[element] = shouldShow; // Assume the update went as expected...

            if (shouldShow != addon->RootNode->IsVisible)
            {
                addon->RootNode->Flags ^= 0x10;
            }

            if (modifyNodeList)
            {
                if (addon->RootNode->IsVisible && addon->UldManager.NodeListCount == 0)
                {
                    addon->UldManager.UpdateDrawNodeList();
                }
                else if (!addon->RootNode->IsVisible && addon->UldManager.NodeListCount != 0)
                {
                    addon->UldManager.NodeListCount = 0;
                }
            }
        }

        private unsafe void UpdateAddons(Dictionary<Element, bool> elements, bool? forcedVisibility = null)
        {
            //PluginLog.Debug($"[{GetType().Name}] UpdateAddons" + (forcedVisibility != null ? (" -> Forced state: " + ((bool)forcedVisibility ? "SHOW" : "HIDE")) : ""));

            AtkStage * stage = AtkStage.GetSingleton();
            if (stage == null) { return;  }

            AtkUnitList* loadedUnitsList = &stage->RaptureAtkUnitManager->AtkUnitManager.AllLoadedUnitsList;
            if (loadedUnitsList == null) { return; }

            AtkUnitBase** addonList = &loadedUnitsList->AtkUnitEntries;
            if (addonList == null) { return; }

            //PluginLog.Debug($"[{GetType().Name}] UpdateAddons -> Iterating through {loadedUnitsList->Count} addon(s)...");
            //foreach (KeyValuePair<Element, bool> expected in _expectedVisibility)
            //{
            //    PluginLog.Debug($"[{GetType().Name}] UpdateAddons -> {expected.Key}: {expected.Value}");
            //}

            for (int i = 0; i < loadedUnitsList->Count; i++)
            {
                AtkUnitBase* addon = addonList[i];
                if (addon == null || addon->RootNode == null || addon->UldManager.LoadedState != 3) { continue; }

                string? name = Marshal.PtrToStringAnsi(new IntPtr(addon->Name));
                if (name == null) { continue; }

                foreach (KeyValuePair<Element, bool> kvp in elements)
                {
                    bool shouldShow = forcedVisibility ?? kvp.Value;

                    if (_elementAddonNames.TryGetValue(kvp.Key, out string? value))
                    {
                        if (name == value)
                        {
                            UpdateAddonVisibility(kvp.Key, addon, shouldShow);
                        }
                    }
                    else
                    {
                        switch (kvp.Key)
                        {
                            case Element.ActionBarLock:
                                // The lock is a CheckBox type child node of _ActionBar!
                                if (name == "_ActionBar")
                                {
                                    if (addon->UldManager.NodeListCount > 0)
                                    {
                                        for (var j = 0; j < addon->UldManager.NodeListCount; j++)
                                        {
                                            AtkResNode* node = addon->UldManager.NodeList[j];
                                            if (node != null && (int)node->Type >= 1000)
                                            {
                                                var compNode = (AtkComponentNode*)node;
                                                var objectInfo = (AtkUldComponentInfo*)compNode->Component->UldManager.Objects;

                                                if (objectInfo->ComponentType == ComponentType.CheckBox)
                                                {
                                                    // This should be the lock!
                                                    if (node->IsVisible != shouldShow)
                                                    {
                                                        node->Flags ^= 0x10;
                                                    }
                                                    _currentVisibility[Element.ActionBarLock] = shouldShow;
                                                    break;
                                                }
                                            }
                                        }
                                    }
                                }
                                break;

                            case Element.Job:
                                if (name.StartsWith("JobHud"))
                                {
                                    UpdateAddonVisibility(kvp.Key, addon, shouldShow);
                                }
                                break;

                            case Element.Chat:
                                if (name.StartsWith("ChatLog"))
                                {
                                    UpdateAddonVisibility(kvp.Key, addon, shouldShow);
                                }
                                break;

                            case Element.TargetInfo:
                                if (name.StartsWith("TargetInfo"))
                                {
                                    UpdateAddonVisibility(kvp.Key, addon, shouldShow);
                                }
                                break;

                            case Element.CrossHotbar:
                                if (name.StartsWith("Action") && name.Contains("Cross"))
                                {
                                    {
                                        UpdateAddonVisibility(kvp.Key, addon, shouldShow);
                                    }
                                }
                                break;

                            default:
                                PluginLog.Debug($"[{GetType().Name}] UpdateAddons: Unsupport UI Element: {kvp.Key}");
                                break;
                        }
                    }
                }
            }
        }
        #endregion

        #region Singleton
        private ElementHider(ElementHiderConfig config) : base(config)
        {
            config.ValueChangeEvent += OnConfigPropertyChanged;
            ConfigurationManager.Instance.ResetEvent += OnConfigReset;

            if (Config.Enabled)
            {
                Enable();
            }
        }

        public static void Initialize() {
            Instance = new ElementHider(ConfigurationManager.Instance.GetConfigObject<ElementHiderConfig>());
        }

        public static ElementHider Instance { get; private set; } = null!;

        ~ElementHider()
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

                case "HideActionBarLock":
                    PluginLog.Debug($"[{this.GetType().Name}] OnConfigPropertyChanged HideActionBarLock: {Config.HideActionBarLock}");
                    if (Config.Enabled)
                    {
                        _expectedVisibility[Element.ActionBarLock] = !Config.HideActionBarLock;
                        UpdateAddons(new Dictionary<Element, bool>() { { Element.ActionBarLock, _expectedVisibility[Element.ActionBarLock] } });
                    }
                    break;
            }
        }

        private void OnConfigReset(ConfigurationManager sender)
        {
            // Configuration doesn't change on reset? 
            PluginLog.Debug($"[{this.GetType().Name}] OnConfigReset");
            Disable();
            _config.ValueChangeEvent -= OnConfigPropertyChanged;
            _config = sender.GetConfigObject<ElementHiderConfig>();
            _config.ValueChangeEvent += OnConfigPropertyChanged;
            Toggle(Config.Enabled);
            PluginLog.Debug($"[{this.GetType().Name}] Config.Enabled: {Config.Enabled}");
        }
        #endregion
    }
}
