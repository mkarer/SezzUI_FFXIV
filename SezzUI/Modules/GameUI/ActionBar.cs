using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SezzUI.Config;
using SezzUI.Interface.GeneralElements;
using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Dalamud.Game.Gui;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using System.Runtime.InteropServices;

namespace SezzUI.Modules.GameUI
{
    public class ActionBar : HudModule
    {
        private ActionBarConfig Config => (ActionBarConfig)_config;
        private readonly Dictionary<Element, Dictionary<uint, Vector2<float>>> _originalPositions = new();
        private static readonly Dictionary<ActionBarLayout, Vector2<byte>> _dimensions = new()
        {
            { ActionBarLayout.H12V1, new(12, 1) },
            { ActionBarLayout.H6V2, new(6, 2) },
            { ActionBarLayout.H4V3, new(4, 3) },
            { ActionBarLayout.H3V4, new(3, 4) },
            { ActionBarLayout.H2V6, new(2, 6) },
            { ActionBarLayout.H1V12, new(1, 12) },
        };
        private static readonly byte _maxButtons = 12;

        public override bool Enable()
        {
            if (!base.Enable()) { return false; }

            EventManager.Game.AddonsLoaded += OnAddonsLoaded;
            EventManager.Game.HudLayoutActivated += OnHudLayoutActivated;

            if (EventManager.Game.IsInGame())
            {
                Update();
            }

            return true;
        }

        public override bool Disable()
        {
            if (!base.Disable()) { return false; }

            EventManager.Game.AddonsLoaded -= OnAddonsLoaded;
            EventManager.Game.HudLayoutActivated -= OnHudLayoutActivated;

            Reset();
            _originalPositions.Clear();

            return true;
        }

        private void Update()
        {
            // TODO
            UpdateActionBar(Element.ActionBar1, Config.Bar1);
            UpdateActionBar(Element.ActionBar2, Config.Bar2);
            UpdateActionBar(Element.ActionBar3, Config.Bar3);
            UpdateActionBar(Element.ActionBar4, Config.Bar4);
            UpdateActionBar(Element.ActionBar5, Config.Bar5);
            UpdateActionBar(Element.ActionBar6, Config.Bar6);
            UpdateActionBar(Element.ActionBar7, Config.Bar7);
            UpdateActionBar(Element.ActionBar8, Config.Bar8);
            UpdateActionBar(Element.ActionBar9, Config.Bar9);
            UpdateActionBar(Element.ActionBar10, Config.Bar10);
        }

        private unsafe void UpdateActionBar(Element bar, SingleActionBarConfig config)
        {
            if (!config.Enabled) { return; }
            
            PluginLog.Debug($"[{GetType().Name}::UpdateActionBar] Updating element: {bar}");

            var addon = (AtkUnitBase*)Plugin.GameGui.GetAddonByName(Addons.Names[bar], 1);
            if (addon != null && addon->RootNode != null)
            {
                int ratio = (int)(Math.Floor(10f * addon->RootNode->Width / addon->RootNode->Height));
                ActionBarLayout layout = ratio switch {
                    86 => ActionBarLayout.H12V1, // 624x72
                    27 => ActionBarLayout.H6V2, // 331x121
                    14 => ActionBarLayout.H4V3, // 241x170
                    6 => ActionBarLayout.H3V4, // 162x260
                    3 => ActionBarLayout.H2V6, // 117x358
                    1 => ActionBarLayout.H1V12, // 72x624
                    _ => ActionBarLayout.Unknown
                };

                switch (layout)
                {
                    case ActionBarLayout.H12V1:
                    case ActionBarLayout.H6V2:
                    case ActionBarLayout.H4V3:
                    case ActionBarLayout.H3V4:
                    case ActionBarLayout.H2V6:
                    case ActionBarLayout.H1V12:
                        PluginLog.Debug($"[{GetType().Name}::UpdateActionBar] Layout: {layout}");

                        bool updateNodes = addon->UldManager.NodeListCount == 0;
                        if (updateNodes)
                        {
                            addon->UldManager.UpdateDrawNodeList();
                        }

                        if (CacheActionBarPositions(bar, (IntPtr)addon))
                        {
                            if (config.InvertRowOrdering) {
                                InvertActionBarRows(bar, (IntPtr)addon, layout);
                            }
                            else
                            {
                                // TODO: Only reset row ordering when other features are added...
                                ResetActionBar(bar, config);
                            }
                        }

                        if (updateNodes)
                        {
                            addon->UldManager.NodeListCount = 0;
                        }
                        break;

                    default:
                        PluginLog.Debug($"[{GetType().Name}::UpdateActionBar] Unknown layout! Ratio: {ratio}");
                        break;
                }
            }
            else
            {
                PluginLog.Debug($"[{GetType().Name}::UpdateActionBar] Invalid addon: {Addons.Names[bar]}");
            }
        }

        private unsafe void InvertActionBarRows(Element bar, IntPtr addonptr, ActionBarLayout layout)
        {
            PluginLog.Debug($"[{GetType().Name}::InvertActionBarRows] {bar}");
            AtkUnitBase* addon = (AtkUnitBase*)addonptr;

            if (_dimensions[layout].Y > 1)
            {
                List<uint> nodeIds = _originalPositions[bar].Keys.ToList();
                nodeIds.Sort();

                for (byte sourceRow = 0; sourceRow < _dimensions[layout].Y / 2; sourceRow++)
                {
                    byte targetRow = (byte)(_dimensions[layout].Y - 1 - sourceRow);
                    PluginLog.Debug($"[{GetType().Name}::InvertActionBarRows] Swapping rows: {sourceRow} <> {targetRow}");

                    for (byte sourceButtonBase = 0; sourceButtonBase < _dimensions[layout].X; sourceButtonBase++)
                    {
                        byte sourceButton = (byte)(sourceButtonBase + _dimensions[layout].X * sourceRow);
                        byte targetButton = (byte)(_dimensions[layout].X * targetRow + sourceButtonBase);
                        PluginLog.Debug($"[{GetType().Name}::InvertActionBarRows] Swapping buttons: {sourceButton} ({nodeIds[sourceButton]}) <> {targetButton} ({nodeIds[targetButton]})");

                        var sourceNode = addon->GetNodeById(nodeIds[sourceButton]);
                        var targetNode = addon->GetNodeById(nodeIds[targetButton]);

                        if (sourceNode != null && targetNode != null)
                        {
                            sourceNode->SetPositionFloat(_originalPositions[bar][nodeIds[targetButton]].X, _originalPositions[bar][nodeIds[targetButton]].Y);
                            targetNode->SetPositionFloat(_originalPositions[bar][nodeIds[sourceButton]].X, _originalPositions[bar][nodeIds[sourceButton]].Y);
                        }
                        else
                        {
                            PluginLog.Debug($"[{GetType().Name}::InvertActionBarRows] Error: Nodes not found!");
                        }
                    }
                }
            }
        }

        private unsafe bool CacheActionBarPositions(Element bar, IntPtr addonptr)
        {
            AtkUnitBase* addon = (AtkUnitBase*)addonptr;

            if (_originalPositions.ContainsKey(bar))
            {
                PluginLog.Debug($"[{GetType().Name}::CacheActionBarPositions] Ignored: {bar} is already cached!");
                return true;
            }
            else if (addon->UldManager.NodeListCount == 0)
            {
                PluginLog.Debug($"[{GetType().Name}::CacheActionBarPositions] Error: {bar} has no child nodes!");
                return false;
            }

            _originalPositions[bar] = new();

            byte buttonsFound = 0;

            for (var j = 0; j < addon->UldManager.NodeListCount; j++)
            {
                AtkResNode* node = addon->UldManager.NodeList[j];
                if (node != null && (int)node->Type >= 1000)
                {
                    var compNode = (AtkComponentNode*)node;
                    var objectInfo = (AtkUldComponentInfo*)compNode->Component->UldManager.Objects;

                    if (objectInfo->ComponentType == ComponentType.Base)
                    {
                        // This should be an ActionButton!
                        PluginLog.Debug($"[{GetType().Name}::CacheActionBarPositions] Caching {bar} node: ID: {node->NodeID} X: {node->X} Y: {node->Y}");
                        _originalPositions[bar].Add(node->NodeID, new(node->X, node->Y));
                        buttonsFound++;
                    }
                }
            }

            if (buttonsFound != _maxButtons)
            {
                PluginLog.Debug($"[{GetType().Name}::CacheActionBarPositions] Invalid amount of buttons found on {bar}: {buttonsFound}/{_maxButtons}");
                _originalPositions.Remove(bar);
                return false;
            }
            else
            {
                return true;
            }
        }

        private void Reset()
        {
            // TODO
            ResetActionBar(Element.ActionBar1, Config.Bar1);
            ResetActionBar(Element.ActionBar2, Config.Bar2);
            ResetActionBar(Element.ActionBar3, Config.Bar3);
            ResetActionBar(Element.ActionBar4, Config.Bar4);
            ResetActionBar(Element.ActionBar5, Config.Bar5);
            ResetActionBar(Element.ActionBar6, Config.Bar6);
            ResetActionBar(Element.ActionBar7, Config.Bar7);
            ResetActionBar(Element.ActionBar8, Config.Bar8);
            ResetActionBar(Element.ActionBar9, Config.Bar9);
            ResetActionBar(Element.ActionBar10, Config.Bar10);
        }

        private unsafe void ResetActionBar(Element bar, SingleActionBarConfig config)
        {
            if (!_originalPositions.ContainsKey(bar)) { return; }

            PluginLog.Debug($"[{GetType().Name}::ResetActionBar] Resetting element: {bar}");

            var addon = (AtkUnitBase*)Plugin.GameGui.GetAddonByName(Addons.Names[bar], 1);
            if (addon != null)
            {
                foreach ((uint nodeId, Vector2<float> pos) in _originalPositions[bar])
                {
                    var node = addon->GetNodeById(nodeId);
                    if (node != null)
                    {
                        node->SetPositionFloat(pos.X, pos.Y);
                    }
                    else
                    {
                        PluginLog.Debug($"[{GetType().Name}::ResetActionBar] Invalid node ID: {nodeId}");
                    }
                }
            }
            else
            {
                PluginLog.Debug($"[{GetType().Name}::ResetActionBar] Invalid addon: {Addons.Names[bar]}");
            }
        }

        private void OnHudLayoutActivated(uint hudLayout, bool ready)
        {
            // Discard cache and update, switching HUD layout resets positions.
            // TODO: This also happens right after logging in (OnAddonsLoaded)?
            _originalPositions.Clear();

            if (ready)
            {
                Update();
            }
        }

        private void OnAddonsLoaded(bool loaded, bool ready)
        {
            if (loaded && ready)
            {
                // Force update!
                Update();
            }
            else
            {
                // Discard cache. ActionBars will reset when the game re-enables addons, we don't have reset them here.
                _originalPositions.Clear();
            }
        }

        #region Singleton
        private ActionBar(ActionBarConfig config) : base(config)
        {
            Config.ValueChangeEvent += OnConfigPropertyChanged;
            Config.Bar1.ValueChangeEvent += OnConfigPropertyChanged;
            Config.Bar2.ValueChangeEvent += OnConfigPropertyChanged;
            Config.Bar3.ValueChangeEvent += OnConfigPropertyChanged;
            Config.Bar4.ValueChangeEvent += OnConfigPropertyChanged;
            Config.Bar5.ValueChangeEvent += OnConfigPropertyChanged;
            Config.Bar6.ValueChangeEvent += OnConfigPropertyChanged;
            Config.Bar7.ValueChangeEvent += OnConfigPropertyChanged;
            Config.Bar8.ValueChangeEvent += OnConfigPropertyChanged;
            Config.Bar9.ValueChangeEvent += OnConfigPropertyChanged;
            Config.Bar10.ValueChangeEvent += OnConfigPropertyChanged;

            ConfigurationManager.Instance.ResetEvent += OnConfigReset;
            Enable();
        }

        public static void Initialize()
        {
            Instance = new ActionBar(ConfigurationManager.Instance.GetConfigObject<ActionBarConfig>());
        }

        public static ActionBar Instance { get; private set; } = null!;

        protected override void InternalDispose()
        {
            Disable();
            Config.Bar1.ValueChangeEvent -= OnConfigPropertyChanged;
            Config.Bar2.ValueChangeEvent -= OnConfigPropertyChanged;
            Config.Bar3.ValueChangeEvent -= OnConfigPropertyChanged;
            Config.Bar4.ValueChangeEvent -= OnConfigPropertyChanged;
            Config.Bar5.ValueChangeEvent -= OnConfigPropertyChanged;
            Config.Bar6.ValueChangeEvent -= OnConfigPropertyChanged;
            Config.Bar7.ValueChangeEvent -= OnConfigPropertyChanged;
            Config.Bar8.ValueChangeEvent -= OnConfigPropertyChanged;
            Config.Bar9.ValueChangeEvent -= OnConfigPropertyChanged;
            Config.Bar10.ValueChangeEvent -= OnConfigPropertyChanged;
            Config.ValueChangeEvent -= OnConfigPropertyChanged;
            ConfigurationManager.Instance.ResetEvent -= OnConfigReset;
        }

        ~ActionBar()
        {
            Dispose(false);
        }
        #endregion

        #region Configuration Events
        private void OnConfigPropertyChanged(object sender, OnChangeBaseArgs args)
        {
            if (sender is SingleActionBarConfig barConfig)
            {
                PluginLog.Debug($"[{sender.GetType().Name}] {barConfig.Bar} OnConfigPropertyChanged {args.PropertyName}: {args}");
                switch (args.PropertyName)
                {
                    case "Enabled":
                        if (barConfig.Enabled)
                        {
                            if (GameEvents.Game.Instance.IsInGame())
                            {
                                // Update bar now.
                                // If not ingame all bars will be updated later anyways.
                                UpdateActionBar(barConfig.Bar, barConfig);
                            }
                        }
                        else
                        {
                            if (GameEvents.Game.Instance.IsInGame())
                            {
                                ResetActionBar(barConfig.Bar, barConfig);
                            }

                            _originalPositions.Remove(barConfig.Bar);
                        }
                        break;

                    case "InvertRowOrdering":
                        if (barConfig.Enabled && GameEvents.Game.Instance.IsInGame())
                        {
                            UpdateActionBar(barConfig.Bar, barConfig);
                        }
                        break;
                }
            }
        }

        private void OnConfigReset(ConfigurationManager sender)
        {
            // Configuration doesn't change on reset? 
            PluginLog.Debug($"[{this.GetType().Name}] OnConfigReset");
            if (_config != null)
            {
                _config.ValueChangeEvent -= OnConfigPropertyChanged;
                Config.Bar1.ValueChangeEvent -= OnConfigPropertyChanged;
                Config.Bar2.ValueChangeEvent -= OnConfigPropertyChanged;
                Config.Bar3.ValueChangeEvent -= OnConfigPropertyChanged;
                Config.Bar4.ValueChangeEvent -= OnConfigPropertyChanged;
                Config.Bar5.ValueChangeEvent -= OnConfigPropertyChanged;
                Config.Bar6.ValueChangeEvent -= OnConfigPropertyChanged;
                Config.Bar7.ValueChangeEvent -= OnConfigPropertyChanged;
                Config.Bar8.ValueChangeEvent -= OnConfigPropertyChanged;
                Config.Bar9.ValueChangeEvent -= OnConfigPropertyChanged;
                Config.Bar10.ValueChangeEvent -= OnConfigPropertyChanged;
            }

            _config = sender.GetConfigObject<ActionBarConfig>();
            _config.ValueChangeEvent += OnConfigPropertyChanged;

            Config.Bar1.ValueChangeEvent += OnConfigPropertyChanged;
            Config.Bar2.ValueChangeEvent += OnConfigPropertyChanged;
            Config.Bar3.ValueChangeEvent += OnConfigPropertyChanged;
            Config.Bar4.ValueChangeEvent += OnConfigPropertyChanged;
            Config.Bar5.ValueChangeEvent += OnConfigPropertyChanged;
            Config.Bar6.ValueChangeEvent += OnConfigPropertyChanged;
            Config.Bar7.ValueChangeEvent += OnConfigPropertyChanged;
            Config.Bar8.ValueChangeEvent += OnConfigPropertyChanged;
            Config.Bar9.ValueChangeEvent += OnConfigPropertyChanged;
            Config.Bar10.ValueChangeEvent += OnConfigPropertyChanged;

            Reset();
            Update();
        }
        #endregion
    }

    internal class Vector2<T>
    {
        public T X { get; set; }
        public T Y { get; set; }

        public Vector2(T x, T y)
        {
            X = x;
            Y = y;
        }
    }
}
