using System;
using Dalamud.Hooking;

namespace SezzUI.Hooking
{
	public sealed class HookWrapper<T> : IHookWrapper where T : Delegate
	{
		private readonly Hook<T> _wrappedHook;
		private bool _disposed;

		public HookWrapper(Hook<T> hook)
		{
			_wrappedHook = hook;
		}

		public void Enable()
		{
			if (_disposed)
			{
				return;
			}

			_wrappedHook.Enable();
		}

		public void Disable()
		{
			if (_disposed)
			{
				return;
			}

			_wrappedHook.Disable();
		}

		public void Dispose()
		{
			Disable();
			_disposed = true;
			_wrappedHook.Dispose();
		}

		public T Original => _wrappedHook.Original;
		public bool IsEnabled => _wrappedHook.IsEnabled;
		public bool IsDisposed => _wrappedHook.IsDisposed;
	}
}