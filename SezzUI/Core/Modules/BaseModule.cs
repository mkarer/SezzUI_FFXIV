using System;
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

		protected bool Enabled { get; private set; }

		/// <summary>
		///     Enabled the module.
		/// </summary>
		/// <returns>TRUE if it wasn't already enabled.</returns>
		internal virtual bool Enable()
		{
			if (!Enabled)
			{
				Logger.Debug("Enable");
				Enabled = true;
				return true;
			}

			Logger.Debug("Enable skipped");
			return false;
		}

		/// <summary>
		///     Disables the module.
		/// </summary>
		/// <returns>TRUE if it wasn't already disabled.</returns>
		internal virtual bool Disable()
		{
			if (Enabled)
			{
				Logger.Debug("Disable");
				Enabled = false;
				return true;
			}

			Logger.Debug("Disable skipped");
			return false;
		}

		/// <summary>
		///     Toggles EnabledState.
		/// </summary>
		/// <param name="enable">EnabledState</param>
		/// <returns>TRUE if state changed.</returns>
		internal virtual bool Toggle(bool enable)
		{
			if (enable != Enabled)
			{
				return enable ? Enable() : Disable();
			}

			return false;
		}

		/// <summary>
		///     Disables and re-enables the module (if it is enabled).
		/// </summary>
		/// <returns>TRUE if module was enabled and was successfully disabled and enabled again.</returns>
		protected virtual bool Reload() => Enabled && Disable() && Enable();

		public virtual void Draw(DrawState state)
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