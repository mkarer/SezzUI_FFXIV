using System;
using Dalamud.Logging;
using Dalamud.Game;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace SezzUI.GameEvents
{
    internal sealed unsafe class Game : BaseGameEvent
    {
        public delegate void AddonsLoadedDelegate(bool loaded, bool ready);
        public event AddonsLoadedDelegate? AddonsLoaded;
        private bool _addonsLoaded = false;
        private bool _addonsReady = false;
        public bool AreAddonsLoaded => _addonsLoaded;
        public bool AreAddonsReady => AreAddonsLoaded && _addonsReady;

        public delegate void AddonVisibilityChangedDelegate(bool visible);
        public event AddonVisibilityChangedDelegate? AddonVisibilityChanged;
        private bool _addonVisibility = false;
        private bool _addonVisibilityCached = false;
        public bool AreAddonsVisible => _addonVisibility;

        public delegate void HudLayoutActivatedDelegate(uint hudLayout, bool ready);
        public event HudLayoutActivatedDelegate? HudLayoutActivated;
        private delegate uint SetHudLayoutDelegate(IntPtr filePtr, uint hudLayout, byte unk0, byte unk1);
        private Hook<SetHudLayoutDelegate>? _setHudLayoutHook;
        private bool _hudLayoutReady = false;
        private static uint UNKNOWN_HUD_LAYOUT = 10;
        private uint _hudLayout = UNKNOWN_HUD_LAYOUT;
     
        #region Singleton
        private static readonly Lazy<Game> ev = new(() => new Game());
        public static Game Instance { get { return ev.Value; } }
        public static bool Initialized { get { return ev.IsValueCreated; } }

        protected override void Initialize()
        {
            try
            {
                if (Plugin.SigScanner.TryScanText("E8 ?? ?? ?? ?? 33 C0 EB 15", out var setHudLayoutPtr))
                {
                    _setHudLayoutHook = new(setHudLayoutPtr, SetHudLayoutDetour);
                    PluginLog.Debug($"[Event:{GetType().Name}] Hooked: SetHudLayout (ptr = {setHudLayoutPtr.ToInt64():X})");
                }
                else
                {
                    PluginLog.Debug($"[Event:{GetType().Name}] Signature not found: SetHudLayout");
                }
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, $"[Event:{GetType().Name}] Failed to setup hooks: {ex}");
            }

            base.Initialize();
        }

        protected override void InternalDispose()
        {
            _setHudLayoutHook?.Dispose();
        }
        #endregion

        public override bool Enable()
        {
            if (base.Enable())
            {
                Plugin.Condition.ConditionChange += OnConditionChange;
                Plugin.Framework.Update += OnFrameworkUpdate;
                Plugin.ClientState.Login += OnLogin;
                Plugin.ClientState.Logout += OnLogout;
                _setHudLayoutHook?.Enable();

                if (IsInGame()) { SetAddonsLoaded(true); }
                return true;
            }

            return false;
        }

        public override bool Disable()
        {
            if (base.Disable())
            {
                Plugin.Condition.ConditionChange -= OnConditionChange;
                Plugin.Framework.Update -= OnFrameworkUpdate;
                Plugin.ClientState.Login -= OnLogin;
                Plugin.ClientState.Logout -= OnLogout;
                _setHudLayoutHook?.Disable();

                _addonsLoaded = false;
                _addonsReady = false;
                _addonVisibility = false;
                _addonVisibilityCached = false;
                _hudLayoutReady = false;
                _hudLayout = UNKNOWN_HUD_LAYOUT;
            }

            return false;
        }

        /// <summary>
        /// Player is in game and addons are loaded.
        /// </summary>
        /// <returns></returns>
        public bool IsInGame()
        {
            return Plugin.ClientState.IsLoggedIn && !Plugin.Condition[ConditionFlag.CreatingCharacter];
        }

        private void SetAddonsLoaded(bool loaded, bool readyStateChanged = false)
        {
            if (_addonsLoaded != loaded || readyStateChanged)
            {
                _addonsLoaded = loaded;
                if (!loaded) { _hudLayout = UNKNOWN_HUD_LAYOUT; }

                try
                {
                    _addonsReady = loaded && (_addonsReady || AreActionBarsLoaded());
                    PluginLog.Debug($"[Event:{GetType().Name}::AddonsLoaded] Loaded: {loaded} Ready: {_addonsReady}");
                    AddonsLoaded?.Invoke(loaded, _addonsReady);
                }
                catch (Exception ex)
                {
                    PluginLog.Error(ex, $"[Event:{GetType().Name}::AddonsLoaded] Failed invoking {nameof(this.AddonsLoaded)}: {ex}");
                }
            }
        }

        private void OnConditionChange(ConditionFlag flag, bool value)
        {
            if (flag == ConditionFlag.CreatingCharacter)
            {
                SetAddonsLoaded(!value);
            }
        }

        private void OnLogin(object? sender, EventArgs e)
        {
                    
            SetAddonsLoaded(true);
        }

        private void OnLogout(object? sender, EventArgs e)
        {
            SetAddonsLoaded(false);
        }

        public bool AreAddonsShown(bool cached = true)
        {
            if (_addonVisibilityCached && cached)
            {
                return _addonVisibility;
            }
            else
            {
                return Plugin.ClientState.IsLoggedIn && !(
                    Plugin.Condition[ConditionFlag.WatchingCutscene] ||
                    Plugin.Condition[ConditionFlag.WatchingCutscene78] ||
                    Plugin.Condition[ConditionFlag.OccupiedInCutSceneEvent] ||
                    Plugin.Condition[ConditionFlag.CreatingCharacter] ||
                    Plugin.Condition[ConditionFlag.BetweenAreas] ||
                    Plugin.Condition[ConditionFlag.BetweenAreas51] ||
                    Plugin.Condition[ConditionFlag.OccupiedSummoningBell] ||
                    Plugin.Condition[ConditionFlag.OccupiedInQuestEvent] ||
                    Plugin.Condition[ConditionFlag.OccupiedInEvent]
                );
            }
        }

        private unsafe bool AreActionBarsLoaded()
        {
            var addon = (AtkUnitBase*)Plugin.GameGui.GetAddonByName(Modules.GameUI.Addons.Names[Modules.GameUI.Element.ActionBar1], 1);
            return (addon != null && addon->UldManager.LoadedState == 3 && addon->RootNode->DrawFlags == 12);
        }

        private void OnFrameworkUpdate(Framework framework)
        {
            bool addonVisibility = AreAddonsShown(false);

            if (_addonsLoaded && (!_addonsReady || (_hudLayout != UNKNOWN_HUD_LAYOUT && !_hudLayoutReady)) && Plugin.ClientState.IsLoggedIn)
            {
                // This is giga bullshit, maybe someday I'm skilled enough to fix this.
                bool addonsReady = AreActionBarsLoaded();
                if (!_addonsReady && addonsReady)
                {
                    _addonsReady = addonsReady;
                    SetAddonsLoaded(true, true);
                }
                if (!_hudLayoutReady && addonsReady)
                {
                    _addonsReady = addonsReady;
                    SetHudLayoutActivated(_hudLayout);
                }
            }

            if (_addonVisibility != addonVisibility)
            {
                _addonVisibility = addonVisibility;
                _addonVisibilityCached = true;

                try
                {
                    PluginLog.Debug($"[Event:{GetType().Name}::AddonVisibilityChanged] State: {addonVisibility}");
                    AddonVisibilityChanged?.Invoke(addonVisibility);
                }
                catch (Exception ex)
                {
                    PluginLog.Error(ex, $"[Event:{GetType().Name}::AddonVisibilityChanged] Failed invoking {nameof(this.AddonVisibilityChanged)}: {ex}");
                }
            }
        }

        private void SetHudLayoutActivated(uint hudLayout)
        {
            try
            {
                _hudLayoutReady = _addonsReady && AreActionBarsLoaded();
                PluginLog.Debug($"[Event:{GetType().Name}::HudLayoutActivated] Layout: {hudLayout} LayoutReady: {_hudLayoutReady}");
                HudLayoutActivated?.Invoke(hudLayout, _hudLayoutReady);
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, $"[Event:{GetType().Name}::HudLayoutActivated] Failed invoking {nameof(this.HudLayoutActivated)}: {ex}");
            }
        }

        private unsafe uint SetHudLayoutDetour(IntPtr filePtr, uint hudLayout, byte unk0, byte unk1)
        {
            uint ret = 177749584; // 177749584 = Layout already active?

            try
            {
                ret = _setHudLayoutHook!.Original(filePtr, hudLayout, unk0, unk1);
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, $"[Event:{GetType().Name}::HudLayoutActivated] Hooked SetHudLayout({filePtr.ToInt64():X}, {hudLayout}, {unk0}, {unk1}) failed: {ex}");
            }

            PluginLog.Debug($"[Event:{GetType().Name}::SetHudLayoutDetour] Layout: {hudLayout} Result: {ret}");
            if (ret == 0)
            {
                _hudLayout = hudLayout;
                SetHudLayoutActivated(hudLayout);
            }

            return ret;
        }
    }
}
