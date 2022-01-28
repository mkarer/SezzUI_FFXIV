using System;
using System.Text;
using Dalamud.Logging;

namespace SezzUI
{
	public class PluginLogger
	{
		private string _logPrefixBase = null!;
		private string _logPrefix = null!;

		public PluginLogger(string prefixBase = "")
		{
			SetPrefix(prefixBase);
		}

		public void SetPrefix(string prefixBase)
		{
			_logPrefixBase = prefixBase;
			_logPrefix = prefixBase != "" ? $"[{prefixBase}] " : "";
		}

		#region Debug

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

		#endregion

		#region Error

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

		#endregion

		#region Warning

		public void Warning(string messageTemplate, params object[] values)
		{
			PluginLog.Warning(new StringBuilder(_logPrefix).Append(messageTemplate).ToString(), values);
		}

		public void Warning(string messagePrefix, string messageTemplate, params object[] values)
		{
			PluginLog.Warning(new StringBuilder("[").Append(_logPrefixBase).Append(_logPrefixBase != "" ? "::" : "").Append(messagePrefix).Append("] ").Append(messageTemplate).ToString(), values);
		}

		public void Warning(Exception exception, string messageTemplate, params object[] values)
		{
			PluginLog.Warning(exception, new StringBuilder(_logPrefix).Append(messageTemplate).ToString(), values);
		}

		public void Warning(Exception exception, string messagePrefix, string messageTemplate, params object[] values)
		{
			PluginLog.Warning(exception, new StringBuilder("[").Append(_logPrefixBase).Append(_logPrefixBase != "" ? "::" : "").Append(messagePrefix).Append("] ").Append(messageTemplate).ToString(), values);
		}

		#endregion
	}
}