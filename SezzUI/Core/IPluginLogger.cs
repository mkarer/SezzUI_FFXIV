using System;
using System.Text;
using Dalamud.Logging;

namespace SezzUI
{
	public interface IPluginLogger
	{
		string LogPrefixBase { get; set; }
		string LogPrefix { get; set; }

		internal sealed void Initialize(string prefixBase)
		{
			LogPrefixBase = prefixBase;
			LogPrefix = $"[{prefixBase}] ";
		}

		internal sealed void Debug(string messageTemplate, params object[] values)
		{
#if DEBUG
			PluginLog.Debug(new StringBuilder(LogPrefix).Append(messageTemplate).ToString(), values);
#endif
		}

		internal sealed void Debug(string messagePrefix, string messageTemplate, params object[] values)
		{
#if DEBUG
			PluginLog.Debug(new StringBuilder("[").Append(LogPrefixBase).Append("::").Append(messagePrefix).Append("] ").Append(messageTemplate).ToString(), values);
#endif
		}

		internal sealed void Debug(Exception exception, string messageTemplate, params object[] values)
		{
#if DEBUG
			PluginLog.Debug(exception, new StringBuilder(LogPrefix).Append(messageTemplate).ToString(), values);
#endif
		}

		internal sealed void Debug(Exception exception, string messagePrefix, string messageTemplate, params object[] values)
		{
#if DEBUG
			PluginLog.Debug(exception, new StringBuilder("[").Append(LogPrefixBase).Append("::").Append(messagePrefix).Append("] ").Append(messageTemplate).ToString(), values);
#endif
		}

		internal sealed void Error(string messageTemplate, params object[] values)
		{
			PluginLog.Error(new StringBuilder(LogPrefix).Append(messageTemplate).ToString(), values);
		}

		internal sealed void Error(string messagePrefix, string messageTemplate, params object[] values)
		{
			PluginLog.Error(new StringBuilder("[").Append(LogPrefixBase).Append("::").Append(messagePrefix).Append("] ").Append(messageTemplate).ToString(), values);
		}

		internal sealed void Error(Exception exception, string messageTemplate, params object[] values)
		{
			PluginLog.Error(exception, new StringBuilder(LogPrefix).Append(messageTemplate).ToString(), values);
		}

		internal sealed void Error(Exception exception, string messagePrefix, string messageTemplate, params object[] values)
		{
			PluginLog.Error(exception, new StringBuilder("[").Append(LogPrefixBase).Append("::").Append(messagePrefix).Append("] ").Append(messageTemplate).ToString(), values);
		}
	}
}