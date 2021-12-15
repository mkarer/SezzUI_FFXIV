using System;
using Dalamud.Logging;
using Dalamud.Game;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.Enums;

namespace SezzUI.GameEvents
{
    public unsafe sealed class Combat : BaseGameEvent
    {
        public event EventHandler? EnteringCombat;
        public event EventHandler? LeavingCombat;

        private static readonly Lazy<Combat> ev = new Lazy<Combat>(() => new Combat());
        public static Combat Instance { get { return ev.Value; } }
        public static bool Initialized { get { return ev.IsValueCreated; } }

        private bool lastState = false;

        public override void Enable()
        {
            if (!Enabled)
            {
                PluginLog.Debug($"[Event:{Name}] Enable");
                Enabled = true;

                Service.Framework.Update += FrameworkUpdate;
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

                Service.Framework.Update -= FrameworkUpdate;
            }
            else
            {
                PluginLog.Debug($"[Event:{Name}] Disable skipped");
            }
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
                state = Service.Condition[ConditionFlag.InCombat] || (treatWeaponOutAsCombat && Service.ClientState.LocalPlayer != null && Service.ClientState.LocalPlayer.StatusFlags.HasFlag(StatusFlags.WeaponOut));
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
                        PluginLog.Debug($"[Event:{Name}] EnteringCombat");
                        EnteringCombat?.Invoke(this, EventArgs.Empty);
                    } else
                    {
                        PluginLog.Debug($"[Event:{Name}] LeavingCombat");
                        LeavingCombat?.Invoke(this, EventArgs.Empty);
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
