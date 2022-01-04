using System;
using Dalamud.Logging;
using Dalamud.Game;
using Dalamud.Game.ClientState.Conditions;

namespace SezzUI.GameEvents
{
    internal sealed unsafe class Game : BaseGameEvent
    {
        public delegate void AddonsLoadedDelegate(bool loaded);
        public event AddonsLoadedDelegate? AddonsLoaded;

        public delegate void AddonVisibilityChangedDelegate(bool visible);
        public event AddonVisibilityChangedDelegate? AddonVisibilityChanged;
        private bool _addonVisibility = false;
        private bool _addonVisibilityCached = false;

        private static readonly Lazy<Game> ev = new(() => new Game());
        public static Game Instance { get { return ev.Value; } }
        public static bool Initialized { get { return ev.IsValueCreated; } }

        public override void Enable()
        {
            if (!Enabled)
            {
                PluginLog.Debug($"[Event:{Name}] Enable");
                Enabled = true;

                Plugin.Condition.ConditionChange += OnConditionChange;
                Plugin.Framework.Update += OnFrameworkUpdate;
            }
            else
            {
                PluginLog.Debug($"[Event:{Name}] Enable skipped");
            }
        }

        public override void Disable()
        {
            if (Enabled)
            {
                PluginLog.Debug($"[Event:{Name}] Disable");
                Enabled = false;

                Plugin.Condition.ConditionChange -= OnConditionChange;
                Plugin.Framework.Update -= OnFrameworkUpdate;
            }
            else
            {
                PluginLog.Debug($"[Event:{Name}] Disable skipped");
            }
        }
        
        public bool IsInGame()
        {
            return Plugin.ClientState.IsLoggedIn && !Plugin.Condition[ConditionFlag.CreatingCharacter];
        }

        private void OnConditionChange(ConditionFlag flag, bool value)
        {
            if (flag == ConditionFlag.CreatingCharacter)
            {
                try
                {
                    PluginLog.Debug($"[{Name}::AddonsLoaded] State: {!value}");
                    AddonsLoaded?.Invoke(!value);
                }
                catch (Exception ex)
                {
                    PluginLog.Error(ex, $"While invoking {nameof(this.AddonsLoaded)}, an exception was thrown.");
                }
            }
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

        private void OnFrameworkUpdate(Framework framework)
        {
            bool addonVisibility = AreAddonsShown(false);

            if (_addonVisibility != addonVisibility)
            {
                _addonVisibility = addonVisibility;
                _addonVisibilityCached = true;

                try
                {
                    PluginLog.Debug($"[{Name}::AddonVisibilityChanged] State: {addonVisibility}");
                    AddonVisibilityChanged?.Invoke(addonVisibility);
                }
                catch (Exception ex)
                {
                    PluginLog.Error(ex, $"While invoking {nameof(this.AddonVisibilityChanged)}, an exception was thrown.");
                }
            }
        }
    }
}
