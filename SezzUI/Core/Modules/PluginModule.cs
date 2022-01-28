using System;
using System.Numerics;
using SezzUI.Config;
using SezzUI.Enums;

namespace SezzUI
{
	/// <summary>
	///     Basic plugin module that can be enabled and disabled and provides logging.
	/// </summary>
	public abstract class PluginModule : IDisposable
	{
		protected PluginConfigObject _config;
		public PluginConfigObject GetConfig() => _config;
		internal PluginLogger Logger;

		protected PluginModule(PluginConfigObject config)
		{
			Logger = new($"HudModule:{GetType().Name}");
			_config = config;
		}

		protected virtual bool Enabled => _isEnabled;
		private bool _isEnabled;

		protected virtual bool Enable()
		{
			if (!_isEnabled)
			{
				Logger.Debug("Enable");
				_isEnabled = true;
				return true;
			}

			Logger.Debug("Enable skipped");
			return false;
		}

		protected virtual bool Disable()
		{
			if (_isEnabled)
			{
				Logger.Debug("Disable");
				_isEnabled = false;
				return true;
			}

			Logger.Debug("Disable skipped");
			return false;
		}

		protected virtual bool Toggle(bool enable)
		{
			if (enable != _isEnabled)
			{
				return enable ? Enable() : Disable();
			}

			return false;
		}

		public virtual void Draw(DrawState state, Vector2? origin)
		{
			// override
		}

		~PluginModule()
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

			if (_isEnabled)
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