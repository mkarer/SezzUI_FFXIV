using System;
using System.Text;
using Dalamud.Logging;

namespace SezzUI
{
    internal abstract class BaseGameEvent : IDisposable
    {
        public virtual bool Enabled { get; protected set; }
        protected string _logPrefix;
        protected string _logPrefixBase;

        public virtual bool Enable()
        {
            if (!Enabled)
            {
                LogDebug("Enable");
                Enabled = true;
                return true;
            }
            else
            {
                LogDebug("Enable", "Not disabled!");
                return false;
            }
        }

        public virtual bool Disable()
        {
            if (Enabled)
            {
                LogDebug("Disable");
                Enabled = false;
                return true;
            }
            else
            {
                LogDebug("Disable", "Not enabled!");
                return false;
            }
        }

        protected BaseGameEvent()
        {
            _logPrefixBase = new StringBuilder("Event:").Append(GetType().Name).ToString();
            _logPrefix = new StringBuilder("[").Append(_logPrefixBase).Append("] ").ToString();

            Initialize();
        }

        protected virtual void Initialize()
        {
            // override
            if (!Enabled) { Enable(); }
        }

        #region Logging
        protected void LogDebug(string messageTemplate, params object[] values)
        {
#if DEBUG
            PluginLog.Debug(new StringBuilder(_logPrefix).Append(messageTemplate).ToString(), values);
#endif
        }

        protected void LogDebug(string messagePrefix, string messageTemplate, params object[] values)
        {
#if DEBUG
            PluginLog.Debug(new StringBuilder("[").Append(_logPrefixBase).Append("::").Append(messagePrefix).Append("] ").Append(messageTemplate).ToString(), values);
#endif
        }

        protected void LogDebug(Exception exception, string messageTemplate, params object[] values)
        {
#if DEBUG
            PluginLog.Debug(exception, new StringBuilder(_logPrefix).Append(messageTemplate).ToString(), values);
#endif
        }

        protected void LogDebug(Exception exception, string messagePrefix, string messageTemplate, params object[] values)
        {
#if DEBUG
            PluginLog.Debug(exception, new StringBuilder("[").Append(_logPrefixBase).Append("::").Append(messagePrefix).Append("] ").Append(messageTemplate).ToString(), values);
#endif
        }

        protected void LogError(string messageTemplate, params object[] values)
        {
            PluginLog.Error(new StringBuilder(_logPrefix).Append(messageTemplate).ToString(), values);
        }

        protected void LogError(string messagePrefix, string messageTemplate, params object[] values)
        {
            PluginLog.Error(new StringBuilder("[").Append(_logPrefixBase).Append("::").Append(messagePrefix).Append("] ").Append(messageTemplate).ToString(), values);
        }

        protected void LogError(Exception exception, string messageTemplate, params object[] values)
        {
            PluginLog.Error(exception, new StringBuilder(_logPrefix).Append(messageTemplate).ToString(), values);
        }

        protected void LogError(Exception exception, string messagePrefix, string messageTemplate, params object[] values)
        {
            PluginLog.Error(exception, new StringBuilder("[").Append(_logPrefixBase).Append("::").Append(messagePrefix).Append("] ").Append(messageTemplate).ToString(), values);
        }
        #endregion

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

            LogDebug("Dispose");
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
