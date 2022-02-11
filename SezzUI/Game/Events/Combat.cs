using System;
using Dalamud.Game;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.Enums;
using SezzUI.Modules;

namespace SezzUI.Game.Events
{
	internal sealed class Combat : BaseEvent
	{
		public Combat()
		{
			(this as IPluginComponent).Enable();
		}

		public event EventHandler? EnteringCombat;
		public event EventHandler? LeavingCombat;

		private bool _lastState;

		protected override void OnEnable()
		{
			_lastState = !IsInCombat();
			Service.Framework.Update += OnFrameworkUpdate;
		}

		protected override void OnDisable()
		{
			Service.Framework.Update -= OnFrameworkUpdate;
		}

		private void OnFrameworkUpdate(Framework framework)
		{
			try
			{
				Update();
			}
			catch (Exception ex)
			{
				Logger.Error(ex);
			}
		}

		public bool IsInCombat(bool treatWeaponOutAsCombat = true)
		{
			bool state = _lastState;
			try
			{
				state = Service.Condition[ConditionFlag.InCombat] || treatWeaponOutAsCombat && Service.ClientState.LocalPlayer != null && Service.ClientState.LocalPlayer.StatusFlags.HasFlag(StatusFlags.WeaponOut);
			}
			catch (Exception ex)
			{
				Logger.Error(ex);
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
						if (Plugin.DebugConfig.LogEvents && Plugin.DebugConfig.LogEventCombatEnteringCombat)
						{
							Logger.Debug("EnteringCombat");
						}
#endif
						try
						{
							EnteringCombat?.Invoke(this, EventArgs.Empty);
						}
						catch (Exception ex)
						{
							Logger.Error($"Failed invoking {nameof(EnteringCombat)}: {ex}");
						}
					}
					else
					{
#if DEBUG
						if (Plugin.DebugConfig.LogEvents && Plugin.DebugConfig.LogEventCombatLeavingCombat)
						{
							Logger.Debug("LeavingCombat");
						}
#endif
						try
						{
							LeavingCombat?.Invoke(this, EventArgs.Empty);
						}
						catch (Exception ex)
						{
							Logger.Error($"Failed invoking {nameof(LeavingCombat)}: {ex}");
						}
					}
				}
			}
			catch (Exception ex)
			{
				Logger.Error(ex);
			}
		}
	}
}