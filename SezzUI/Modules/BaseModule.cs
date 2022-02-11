using System;
using SezzUI.Enums;
using SezzUI.Hooking;
using SezzUI.Logging;

namespace SezzUI.Modules
{
	/// <summary>
	///     Very basic plugin module that can be enabled and disabled and provides logging.
	/// </summary>
	public abstract class BaseModule : IPluginComponent, IPluginLogger
	{
		protected PluginLogger Logger { get; private set; }

		PluginLogger IPluginLogger.Logger
		{
			get => Logger;
			set => Logger = value;
		}

		bool IPluginComponent.IsEnabled { get; set; }
		bool IPluginComponent.CanLoad { get; set; } = true;
		bool IPluginDisposable.IsDisposed { get; set; } = false;

		protected BaseModule()
		{
			Logger = new($"BaseModule:{GetType().Name}");
		}

		#region Overrides

		protected virtual void OnDraw(DrawState state)
		{
		}

		protected virtual void OnEnable()
		{
		}

		protected virtual void OnDisable()
		{
		}

		protected virtual void OnDispose()
		{
		}

		#endregion

		#region IPluginComponent Methods

		void IPluginComponent.OnEnable()
		{
			(this as IHookAccessor)?.EnableHooks();
			OnEnable();
		}

		void IPluginComponent.OnDisable()
		{
			(this as IHookAccessor)?.DisableHooks();
			OnDisable();
		}

		public void Draw(DrawState state)
		{
			OnDraw(state);
		}

		~BaseModule()
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
			if (!disposing || (this as IPluginDisposable).IsDisposed)
			{
				return;
			}

			(this as IPluginComponent).Disable();
			(this as IHookAccessor)?.DisposeHooks();
			OnDispose();
			(this as IPluginDisposable).IsDisposed = true;
		}

		#endregion
	}
}