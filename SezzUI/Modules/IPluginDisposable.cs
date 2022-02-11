using System;

namespace SezzUI.Modules
{
	public interface IPluginDisposable : IDisposable
	{
		bool IsDisposed { get; internal set; }
	}
}