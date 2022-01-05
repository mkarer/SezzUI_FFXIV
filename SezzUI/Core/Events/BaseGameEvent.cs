using System;
using Dalamud.Logging;

namespace SezzUI
{
    internal abstract class BaseGameEvent : IDisposable
    {
        public virtual bool Enabled { get; protected set; }

        public virtual bool Enable()
        {
            if (!Enabled)
            {
                PluginLog.Debug($"[Event:{GetType().Name}] Enable");
                Enabled = true;
                return true;
            }
            else
            {
                PluginLog.Debug($"[Event:{GetType().Name}] Enable skipped");
                return false;
            }
        }

        public virtual bool Disable()
        {
            if (Enabled)
            {
                PluginLog.Debug($"[Event:{GetType().Name}] Disable");
                Enabled = false;
                return true;
            }
            else
            {
                PluginLog.Debug($"[Event:{GetType().Name}] Disable skipped");
                return false;
            }
        }

        protected BaseGameEvent()
        {
            Initialize();
        }

        protected virtual void Initialize()
        {
            // override
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

            PluginLog.Debug($"[Event:{GetType().Name}] Dispose");
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
