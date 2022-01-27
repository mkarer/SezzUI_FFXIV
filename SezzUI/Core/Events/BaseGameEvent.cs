using System;

namespace SezzUI
{
	internal abstract class BaseGameEvent : IDisposable
	{
		public virtual bool Enabled { get; protected set; }
		internal PluginLogger Logger;

		public virtual bool Enable()
		{
			if (!Enabled)
			{
				Logger.Debug("Enable");
				Enabled = true;
				return true;
			}

			Logger.Debug("Enable", "Not disabled!");
			return false;
		}

		public virtual bool Disable()
		{
			if (Enabled)
			{
				Logger.Debug("Disable");
				Enabled = false;
				return true;
			}

			Logger.Debug("Disable", "Not enabled!");
			return false;
		}

		protected BaseGameEvent()
		{
			Logger = new($"Event:{GetType().Name}");
			Initialize();
		}

		protected virtual void Initialize()
		{
			// override
			if (!Enabled)
			{
				Enable();
			}
		}

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

			Logger.Debug("Dispose");
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