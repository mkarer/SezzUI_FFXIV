using System;

namespace SezzUI.Hooking
{
	public interface IHookWrapper : IDisposable
	{
		public void Enable();
		public void Disable();

		public bool IsEnabled { get; }
		public bool IsDisposed { get; }
	}
}