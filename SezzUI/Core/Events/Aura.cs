using System;
using System.Collections.Generic;
using Dalamud.Logging;
using Dalamud.Game;
using Dalamud.Game.ClientState.Objects.SubKinds;

namespace SezzUI.GameEvents
{
    public enum AuraTarget
    {
        Player = 0,
        Target = 1
    }

    public class AuraData
    {
        public uint StatusId;
    }

    public class AuraEventArgs : EventArgs
    {
        public AuraEventArgs(AuraTarget unit, uint statusId)
        {
            Unit = unit;
            StatusId = statusId;
        }

        public AuraTarget Unit { get; set; }
        public uint StatusId { get; set; }
    }

    public unsafe sealed class Aura : BaseGameEvent
    {
        public event EventHandler<AuraEventArgs>? AuraApplied;
        public event EventHandler<AuraEventArgs>? AuraChanged;
        public event EventHandler<AuraEventArgs>? AuraRemoved;

        private static readonly Lazy<Aura> ev = new Lazy<Aura>(() => new Aura());
        public static Aura Instance { get { return ev.Value; } }
        public static bool Initialized { get { return ev.IsValueCreated; } }

        private Dictionary<AuraTarget, Dictionary<uint, AuraData>> _cache;
        private Dictionary<AuraTarget, List<uint>> _watched;

        public Aura()
        {
            _cache = new();
            _cache.Add(AuraTarget.Player, new());
            _cache.Add(AuraTarget.Target, new());

            _watched = new();
            _watched.Add(AuraTarget.Player, new());
            _watched.Add(AuraTarget.Target, new());
        }

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

        private void Update()
        {
            try
            {
                PlayerCharacter? player = Service.ClientState.LocalPlayer;
            }
            catch
            {
                // 
            }
        }
    }
}
