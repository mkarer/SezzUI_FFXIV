using System;
using Dalamud.Logging;

namespace SezzUI
{
    public abstract class BaseGameEvent : IDisposable
    {
        public virtual string Name => GetType().Name;
        public virtual bool Enabled { get; protected set; }

        public virtual void Enable()
        {
            if (!Enabled)
            {
                PluginLog.Debug($"[Event:{Name}] Enable");
                Enabled = true;
            }
            else
            {
                PluginLog.Debug($"[Event:{Name}] Enable skipped");
            }
        }

        public virtual void Disable()
        {
            if (Enabled)
            {
                PluginLog.Debug($"[Event:{Name}] Disable");
                Enabled = false;
            }
            else
            {
                PluginLog.Debug($"[Event:{Name}] Disable skipped");
            }
        }

        protected BaseGameEvent()
        {
            if (!Enabled) Enable();
        }

        public virtual void Dispose()
        {
            PluginLog.Debug($"[Event:{Name}] Dispose");

            if (Enabled)
            {
                Disable();
            }
        }
    }
}
