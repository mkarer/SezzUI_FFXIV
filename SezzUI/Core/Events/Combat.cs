using System;
using Dalamud.Game;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.Enums;

namespace SezzUI.GameEvents
{
	internal sealed class Combat : BaseGameEvent
	{
		public event EventHandler? EnteringCombat;
		public event EventHandler? LeavingCombat;

		private bool _lastState;

		#region Singleton

		private static readonly Lazy<Combat> _ev = new(() => new());
		public static Combat Instance => _ev.Value;
		public static bool Initialized => _ev.IsValueCreated;

		#endregion

		public override bool Enable()
		{
			if (base.Enable())
			{
				_lastState = !IsInCombat();
				Plugin.Framework.Update += OnFrameworkUpdate;
				return true;
			}

			return false;
		}

		public override bool Disable()
		{
			if (base.Disable())
			{
				Plugin.Framework.Update -= OnFrameworkUpdate;
				return true;
			}

			return false;
		}

		private void OnFrameworkUpdate(Framework framework)
		{
			try
			{
				Update();
			}
			catch (Exception ex)
			{
				LogError(ex, "OnFrameworkUpdate", $"Error: {ex}");
			}
		}

		public bool IsInCombat(bool treatWeaponOutAsCombat = true)
		{
			bool state = _lastState;
			try
			{
				state = Plugin.Condition[ConditionFlag.InCombat] || treatWeaponOutAsCombat && Plugin.ClientState.LocalPlayer != null && Plugin.ClientState.LocalPlayer.StatusFlags.HasFlag(StatusFlags.WeaponOut);
			}
			catch (Exception ex)
			{
				LogError(ex, "IsInCombat", $"Error: {ex}");
			}

			return state;
		}

		private void Update()
		{
			try
			{
				bool state = IsInCombat();
				if (state != _lastState)
				{
					_lastState = state;
					if (state)
					{
#if DEBUG
						if (EventManager.Config.LogEvents && EventManager.Config.LogEventCombatEnteringCombat)
						{
							LogDebug("EnteringCombat");
						}
#endif
						try
						{
							EnteringCombat?.Invoke(this, EventArgs.Empty);
						}
						catch (Exception ex)
						{
							LogError(ex, "EnteringCombat", $"Failed invoking {nameof(EnteringCombat)}: {ex}");
						}
					}
					else
					{
#if DEBUG
						if (EventManager.Config.LogEvents && EventManager.Config.LogEventCombatLeavingCombat)
						{
							LogDebug("LeavingCombat");
						}
#endif
						try
						{
							LeavingCombat?.Invoke(this, EventArgs.Empty);
						}
						catch (Exception ex)
						{
							LogError(ex, "LeavingCombat", $"Failed invoking {nameof(LeavingCombat)}: {ex}");
						}
					}
				}
			}
			catch (Exception ex)
			{
				LogError(ex, "Update", $"Error: {ex}");
			}
		}
	}
}