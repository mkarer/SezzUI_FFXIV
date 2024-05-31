using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace SezzUI.Logging;

public class PluginLogger
{
	private string _prefix = "";

	public PluginLogger(string prefix = "")
	{
		SetPrefix(prefix);
	}

	public void SetPrefix(string prefix)
	{
		_prefix = prefix;
	}

#if DEBUG
	public void Verbose(object message, [CallerFilePath] string callerPath = "", [CallerMemberName] string callerName = "", [CallerLineNumber] int lineNumber = -1)
	{
		foreach (string m in SplitMessage(message))
		{
			Services.PluginLog.Verbose(new StringBuilder("[").Append(_prefix).Append(_prefix != "" ? "::" : "").Append(callerName).Append(':').Append(lineNumber).Append("] ").Append(m).ToString());
		}
	}
#else
		public void Verbose(object message)
		{
			foreach (string m in SplitMessage(message))
			{
				Service.PluginLog.Verbose(new StringBuilder().Append(_prefix != "" ? $"[{_prefix}] " : "").Append(m).ToString());
			}
		}
#endif

#if DEBUG
	public void Debug(object message, [CallerFilePath] string callerPath = "", [CallerMemberName] string callerName = "", [CallerLineNumber] int lineNumber = -1)
	{
		foreach (string m in SplitMessage(message))
		{
			Services.PluginLog.Debug(new StringBuilder("[").Append(_prefix).Append(_prefix != "" ? "::" : "").Append(callerName).Append(':').Append(lineNumber).Append("] ").Append(m).ToString());
		}
	}
#else
		public void Debug(object message)
		{
			foreach (string m in SplitMessage(message))
			{
				Service.PluginLog.Debug(new StringBuilder().Append(_prefix != "" ? $"[{_prefix}] " : "").Append(m).ToString());
			}
		}
#endif

#if DEBUG
	public void Information(object message, [CallerFilePath] string callerPath = "", [CallerMemberName] string callerName = "", [CallerLineNumber] int lineNumber = -1)
	{
		foreach (string m in SplitMessage(message))
		{
			Services.PluginLog.Information(new StringBuilder("[").Append(_prefix).Append(_prefix != "" ? "::" : "").Append(callerName).Append(':').Append(lineNumber).Append("] ").Append(m).ToString());
		}
	}
#else
		public void Information(object message)
		{
			foreach (string m in SplitMessage(message))
			{
				Service.PluginLog.Information(new StringBuilder().Append(_prefix != "" ? $"[{_prefix}] " : "").Append(m).ToString());
			}
		}
#endif

#if DEBUG
	public void Warning(object message, [CallerFilePath] string callerPath = "", [CallerMemberName] string callerName = "", [CallerLineNumber] int lineNumber = -1)
	{
		foreach (string m in SplitMessage(message))
		{
			Services.PluginLog.Warning(new StringBuilder("[").Append(_prefix).Append(_prefix != "" ? "::" : "").Append(callerName).Append(':').Append(lineNumber).Append("] ").Append(m).ToString());
		}
	}
#else
		public void Warning(object message)
		{
			foreach (string m in SplitMessage(message))
			{
				Service.PluginLog.Warning(new StringBuilder().Append(_prefix != "" ? $"[{_prefix}] " : "").Append(m).ToString());
			}
		}
#endif

#if DEBUG
	public void Error(object message, [CallerFilePath] string callerPath = "", [CallerMemberName] string callerName = "", [CallerLineNumber] int lineNumber = -1)
	{
		foreach (string m in SplitMessage(message))
		{
			Services.PluginLog.Error(new StringBuilder("[").Append(_prefix).Append(_prefix != "" ? "::" : "").Append(callerName).Append(':').Append(lineNumber).Append("] ").Append(m).ToString());
		}
	}
#else
		public void Error(object message)
		{
			foreach (string m in SplitMessage(message))
			{
				Service.PluginLog.Error(new StringBuilder().Append(_prefix != "" ? $"[{_prefix}] " : "").Append(m).ToString());
			}
		}
#endif

#if DEBUG
	public void Fatal(object message, [CallerFilePath] string callerPath = "", [CallerMemberName] string callerName = "", [CallerLineNumber] int lineNumber = -1)
	{
		foreach (string m in SplitMessage(message))
		{
			Services.PluginLog.Fatal(new StringBuilder("[").Append(_prefix).Append(_prefix != "" ? "::" : "").Append(callerName).Append(':').Append(lineNumber).Append("] ").Append(m).ToString());
		}
	}
#else
		public void Fatal(object message)
		{
			foreach (string m in SplitMessage(message))
			{
				Service.PluginLog.Fatal(new StringBuilder().Append(_prefix != "" ? $"[{_prefix}] " : "").Append(m).ToString());
			}
		}
#endif

	private static IEnumerable<string> SplitMessage(object message)
	{
		if (message is IList list)
		{
			return list.Cast<object>().Select((t, i) => $"{i}: {t}");
		}

		return $"{message}".Split('\n');
	}
}