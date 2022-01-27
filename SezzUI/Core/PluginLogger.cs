using System;
using System.Text;
using Dalamud.Logging;

namespace SezzUI
{
	public class PluginLogger
	{
		private readonly string _logPrefixBase;
		private readonly string _logPrefix;

		public PluginLogger(string prefixBase = "")
		{
			_logPrefixBase = prefixBase;
			_logPrefix = prefixBase != "" ? $"[{prefixBase}] " : "";
		}

		public void Debug(string messageTemplate, params object[] values)
		{
#if DEBUG
			PluginLog.Debug(new StringBuilder(_logPrefix).Append(messageTemplate).ToString(), values);
#endif
		}

		public void Debug(string messagePrefix, string messageTemplate, params object[] values)
		{
#if DEBUG
			PluginLog.Debug(new StringBuilder("[").Append(_logPrefixBase).Append(_logPrefixBase != "" ? "::" : "").Append(messagePrefix).Append("] ").Append(messageTemplate).ToString(), values);
#endif
		}

		public void Debug(Exception exception, string messageTemplate, params object[] values)
		{
#if DEBUG
			PluginLog.Debug(exception, new StringBuilder(_logPrefix).Append(messageTemplate).ToString(), values);
#endif
		}

		public void Debug(Exception exception, string messagePrefix, string messageTemplate, params object[] values)
		{
#if DEBUG
			PluginLog.Debug(exception, new StringBuilder("[").Append(_logPrefixBase).Append(_logPrefixBase != "" ? "::" : "").Append(messagePrefix).Append("] ").Append(messageTemplate).ToString(), values);
#endif
		}

		public void Error(string messageTemplate, params object[] values)
		{
			PluginLog.Error(new StringBuilder(_logPrefix).Append(messageTemplate).ToString(), values);
		}

		public void Error(string messagePrefix, string messageTemplate, params object[] values)
		{
			PluginLog.Error(new StringBuilder("[").Append(_logPrefixBase).Append(_logPrefixBase != "" ? "::" : "").Append(messagePrefix).Append("] ").Append(messageTemplate).ToString(), values);
		}

		public void Error(Exception exception, string messageTemplate, params object[] values)
		{
			PluginLog.Error(exception, new StringBuilder(_logPrefix).Append(messageTemplate).ToString(), values);
		}

		public void Error(Exception exception, string messagePrefix, string messageTemplate, params object[] values)
		{
			PluginLog.Error(exception, new StringBuilder("[").Append(_logPrefixBase).Append(_logPrefixBase != "" ? "::" : "").Append(messagePrefix).Append("] ").Append(messageTemplate).ToString(), values);
		}
	}
}