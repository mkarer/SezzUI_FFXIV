using System;
using Dalamud.Logging;

namespace SezzUI
{
    internal abstract class BaseGameEvent : IDisposable
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
            if (!Enabled) { Enable(); }
        }

        ~BaseGameEvent()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }

            PluginLog.Debug($"[Event:{Name}] Dispose");
            if (Enabled)
            {
                Disable();
            }

            InternalDispose();
        }

        protected virtual void InternalDispose()
        {
            // override
        }
    }
}
