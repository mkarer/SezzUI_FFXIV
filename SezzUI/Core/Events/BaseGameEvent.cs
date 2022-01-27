using System;

namespace SezzUI
{
	internal abstract class BaseGameEvent : IPluginLogger, IDisposable
	{
		public virtual bool Enabled { get; protected set; }

		#region Logger

		string IPluginLogger.LogPrefixBase { get; set; } = null!;

		string IPluginLogger.LogPrefix { get; set; } = null!;

		internal IPluginLogger Logger => this;

		#endregion

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
			Logger.Initialize($"Event::{GetType().Name}");
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