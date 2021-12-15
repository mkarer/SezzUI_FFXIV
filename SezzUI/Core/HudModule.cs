using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dalamud.Logging;
using Dalamud.Plugin;
using System.Numerics;

namespace SezzUI
{
    public abstract class HudModule : IDisposable
    {
        public abstract string Name { get; }
        public virtual string Key => GetType().Name;
        public virtual string? Description => null;
        public virtual bool Enabled { get; protected set; }

        public virtual void Enable()
        {
            if (!Enabled)
            {
                PluginLog.Debug($"[HudModule:{Name}] Enable");
                Enabled = true;
            }
            else
            {
                PluginLog.Debug($"[HudModule:{Name}] Enable skipped");
            }
        }

        public virtual void Disable()
        {
            if (Enabled)
            {
                PluginLog.Debug($"[HudModule:{Name}] Disable");
                Enabled = false;
            }
            else
            {
                PluginLog.Debug($"[HudModule:{Name}] Disable skipped");
            }
        }

        public virtual void Draw(Vector2 origin)
        {
        }

        public virtual void Dispose()
        {
            PluginLog.Debug($"[HudModule:{Name}] Dispose");

            if (Enabled)
            {
                Disable();
            }
        }
    }
}
