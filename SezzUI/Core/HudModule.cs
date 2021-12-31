using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dalamud.Logging;
using Dalamud.Plugin;
using System.Numerics;
using SezzUI.Config;

namespace SezzUI
{
    public abstract class HudModule : IDisposable
    {
        protected PluginConfigObject _config;
        public PluginConfigObject GetConfig() { return _config; }

        public HudModule(PluginConfigObject config)
        {
            _config = config;
        }

        public virtual bool Enabled {
            get
            {
                return _isEnabled;
            }
        }
        private bool _isEnabled = false;

        public virtual bool Enable()
        {
            if (!_isEnabled)
            {
                PluginLog.Debug($"[HudModule:{GetType().Name}] Enable");
                _isEnabled = true;
                return true;
            }
            else
            {
                PluginLog.Debug($"[HudModule:{GetType().Name}] Enable skipped");
                return false;
            }
        }

        public virtual bool Disable()
        {
            if (_isEnabled)
            {
                PluginLog.Debug($"[HudModule:{GetType().Name}] Disable");
                _isEnabled = false;
                return true;
            }
            else
            {
                PluginLog.Debug($"[HudModule:{GetType().Name}] Disable skipped");
                return false;
            }
        }

        public virtual void Draw(Vector2 origin)
        {
        }

        ~HudModule()
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

            PluginLog.Debug($"[HudModule:{GetType().Name}] Dispose");

            if (_isEnabled)
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
