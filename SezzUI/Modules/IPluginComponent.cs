namespace SezzUI.Modules;

/// <summary>
///     Can be enabled, disabled and disposed.
/// </summary>
public interface IPluginComponent : IPluginDisposable
{
	bool IsEnabled { get; protected set; }
	bool CanLoad { get; set; }

	protected void OnEnable();
	protected void OnDisable();

	/// <summary>
	///     Enables component.
	/// </summary>
	/// <returns>TRUE if successful, FALSE if already enabled.</returns>
	bool Enable()
	{
		if (!CanLoad || IsEnabled)
		{
			return false;
		}

		IsEnabled = true;
		OnEnable();
		return true;
	}

	/// <summary>
	///     Disables component.
	/// </summary>
	/// <returns>TRUE if successful, FALSE if already disabled.</returns>
	bool Disable()
	{
		if (!IsEnabled)
		{
			return false;
		}

		IsEnabled = false;
		OnDisable();
		return true;
	}

	/// <summary>
	///     Sets enabled state.
	/// </summary>
	/// <returns>TRUE if successful, FALSE if state didn't change.</returns>
	bool SetEnabledState(bool enable) => enable ? Enable() : Disable();

	/// <summary>
	///     Disables and re-enables the component (only if it is currently enabled).
	/// </summary>
	/// <returns>TRUE if component was enabled and could successfully be disabled and re-enabled.</returns>
	bool Reload() => IsEnabled && Disable() && Enable();

	/// <summary>
	///     Toggles enable state.
	/// </summary>
	/// <returns>TRUE if successful.</returns>
	bool Toggle() => SetEnabledState(!IsEnabled);
}