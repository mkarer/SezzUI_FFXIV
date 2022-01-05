using System;
using Dalamud.Logging;
using Dalamud.Game;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.Enums;

namespace SezzUI.GameEvents
{
    internal sealed unsafe class Combat : BaseGameEvent
    {
        public event EventHandler? EnteringCombat;
        public event EventHandler? LeavingCombat;

        private static readonly Lazy<Combat> ev = new Lazy<Combat>(() => new Combat());
        public static Combat Instance { get { return ev.Value; } }
        public static bool Initialized { get { return ev.IsValueCreated; } }

        private bool lastState = false;

        public override bool Enable()
        {
            if (base.Enable())
            {
                Plugin.Framework.Update += FrameworkUpdate;
                return true;
            }
            
            return false;
        }

        public override bool Disable()
        {
            if (base.Disable())
            {
                Plugin.Framework.Update -= FrameworkUpdate;
                return true;
            }

            return false;
        }

        private void FrameworkUpdate(Framework framework)
        {
            try
            {
                Update();
            }
            catch
            {
                // 
            }
        }

        public bool IsInCombat(bool treatWeaponOutAsCombat = true)
        {
            bool state = lastState;
            try
            {
                state = Plugin.Condition[ConditionFlag.InCombat] || (treatWeaponOutAsCombat && Plugin.ClientState.LocalPlayer != null && Plugin.ClientState.LocalPlayer.StatusFlags.HasFlag(StatusFlags.WeaponOut));
            }
            catch
            {
                // 
            }

            return state;
        }

        private void Update()
        {
            try
            {
                bool state = IsInCombat();
                if (state != lastState)
                {
                    lastState = state;
                    if (state)
                    {
                        PluginLog.Debug($"[Event:{GetType().Name}] EnteringCombat");
                        try
                        {
                            EnteringCombat?.Invoke(this, EventArgs.Empty);
                        }
                        catch (Exception ex)
                        {
                            PluginLog.Error(ex, $"[Event:{GetType().Name}::EnteringCombat] Failed invoking {nameof(this.EnteringCombat)}: {ex}");
                        }
                    } else
                    {
                        PluginLog.Debug($"[Event:{GetType().Name}] LeavingCombat");
                        try
                        {
                            LeavingCombat?.Invoke(this, EventArgs.Empty);
                        }
                        catch (Exception ex)
                        {
                            PluginLog.Error(ex, $"[Event:{GetType().Name}::LeavingCombat] Failed invoking {nameof(this.LeavingCombat)}: {ex}");
                        }
                    }
                }
            }
            catch
            {
                // 
            }
        }
    }
}
