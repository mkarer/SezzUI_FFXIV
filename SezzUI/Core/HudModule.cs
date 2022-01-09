using System;
using System.Text;
using Dalamud.Logging;
using System.Numerics;
using SezzUI.Config;
using SezzUI.Enums;

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
                LogDebug("Enable");
                _isEnabled = true;
                return true;
            }
            else
            {
                LogDebug("Enable skipped");
                return false;
            }
        }

        public virtual bool Disable()
        {
            if (_isEnabled)
            {
                LogDebug("Disable");
                _isEnabled = false;
                return true;
            }
            else
            {
                LogDebug("Disable skipped");
                return false;
            }
        }

        public virtual bool Toggle(bool enable)
        {
            if (enable != _isEnabled)
            {
                return enable ? Enable() : Disable();
            }
            return false;
        }

        protected void LogDebug(string messageTemplate, params object[] values)
        {
#if DEBUG
            PluginLog.Debug(new StringBuilder("[HudModule:").Append(GetType().Name).Append("] ").Append(messageTemplate).ToString(), values);
#endif
        }

        protected void LogDebug(Exception exception, string messageTemplate, params object[] values)
        {
#if DEBUG
            PluginLog.Debug(exception, new StringBuilder("[HudModule:").Append(GetType().Name).Append("] ").Append(messageTemplate).ToString(), values);
#endif
        }

        public virtual void Draw(DrawState state, Vector2? origin)
        {
            // override
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

            LogDebug("Dispose");

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
