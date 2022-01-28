using System;
using System.Numerics;
using SezzUI.Enums;

namespace SezzUI.Modules
{
	/// <summary>
	///     Very basic plugin module that can be enabled and disabled and provides logging.
	/// </summary>
	public abstract class BaseModule : IDisposable
	{
		internal PluginLogger Logger;

		protected BaseModule()
		{
			Logger = new($"BaseModule:{GetType().Name}");
		}

		protected bool Enabled => _isEnabled;
		private bool _isEnabled;

		/// <summary>
		/// Enabled the module.
		/// </summary>
		/// <returns>TRUE if it wasn't already enabled.</returns>
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

		/// <summary>
		/// Disables the module.
		/// </summary>
		/// <returns>TRUE if it wasn't already disabled.</returns>
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

		/// <summary>
		/// Toggles EnabledState.
		/// </summary>
		/// <param name="enable">EnabledState</param>
		/// <returns>TRUE if state changed.</returns>
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
			// Override
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