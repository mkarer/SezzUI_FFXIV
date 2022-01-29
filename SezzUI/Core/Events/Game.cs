using System;
using Dalamud.Game;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Component.GUI;
using SezzUI.Enums;
using SezzUI.Modules.GameUI;

namespace SezzUI.GameEvents
{
	internal sealed unsafe class Game : BaseGameEvent
	{
		public delegate void AddonsLoadedDelegate(bool loaded, bool ready);

		public event AddonsLoadedDelegate? AddonsLoaded;
		private bool _addonsReady;
		public bool AreAddonsLoaded { get; private set; }

		public bool AreAddonsReady => AreAddonsLoaded && _addonsReady;

		public delegate void AddonsVisibilityChangedDelegate(bool visible);

		public event AddonsVisibilityChangedDelegate? AddonsVisibilityChanged;
		private bool _addonsVisibilityCached;
		public bool AreAddonsVisible { get; private set; }

		public delegate void HudLayoutActivatedDelegate(uint hudLayout, bool ready);

		public event HudLayoutActivatedDelegate? HudLayoutActivated;

		private delegate uint SetHudLayoutDelegate(IntPtr filePtr, uint hudLayout, byte unk0, byte unk1);

		private Hook<SetHudLayoutDelegate>? _setHudLayoutHook;
		private bool _hudLayoutReady;
		private static readonly uint UNKNOWN_HUD_LAYOUT = 10;
		private uint _hudLayout = UNKNOWN_HUD_LAYOUT;

		#region Singleton

		private static readonly Lazy<Game> ev = new(() => new());
		public static Game Instance => ev.Value;
		public static bool Initialized => ev.IsValueCreated;

		protected override void Initialize()
		{
			try
			{
				if (Plugin.SigScanner.TryScanText("E8 ?? ?? ?? ?? 33 C0 EB 15", out IntPtr setHudLayoutPtr))
				{
					_setHudLayoutHook = new(setHudLayoutPtr, SetHudLayoutDetour);
#if DEBUG
					if (EventManager.Config.LogEvents && EventManager.Config.LogEventGame)
					{
						Logger.Debug($"Hooked: SetHudLayout (ptr = {setHudLayoutPtr.ToInt64():X})");
					}
#endif
				}
				else
				{
					Logger.Error("Signature not found: SetHudLayout");
				}
			}
			catch (Exception ex)
			{
				Logger.Error(ex, $"Failed to setup hooks: {ex}");
			}

			base.Initialize();
		}

		protected override void InternalDispose()
		{
			_setHudLayoutHook?.Dispose();
		}

		#endregion

		public override bool Enable()
		{
			if (base.Enable())
			{
				Plugin.Condition.ConditionChange += OnConditionChange;
				Plugin.Framework.Update += OnFrameworkUpdate;
				Plugin.ClientState.Login += OnLogin;
				Plugin.ClientState.Logout += OnLogout;
				_setHudLayoutHook?.Enable();

				if (IsInGame())
				{
					SetAddonsLoaded(true);
				}

				return true;
			}

			return false;
		}

		public override bool Disable()
		{
			if (base.Disable())
			{
				Plugin.Condition.ConditionChange -= OnConditionChange;
				Plugin.Framework.Update -= OnFrameworkUpdate;
				Plugin.ClientState.Login -= OnLogin;
				Plugin.ClientState.Logout -= OnLogout;
				_setHudLayoutHook?.Disable();

				AreAddonsLoaded = false;
				_addonsReady = false;
				AreAddonsVisible = false;
				_addonsVisibilityCached = false;
				_hudLayoutReady = false;
				_hudLayout = UNKNOWN_HUD_LAYOUT;

				return true;
			}

			return false;
		}

		/// <summary>
		///     Player is in game and addons are loaded.
		/// </summary>
		/// <returns></returns>
		public bool IsInGame() => Plugin.ClientState.IsLoggedIn && !Plugin.Condition[ConditionFlag.CreatingCharacter];

		private void SetAddonsLoaded(bool loaded, bool readyStateChanged = false)
		{
			if (AreAddonsLoaded != loaded || readyStateChanged)
			{
				AreAddonsLoaded = loaded;
				if (!loaded)
				{
					_hudLayout = UNKNOWN_HUD_LAYOUT;
				}

				try
				{
					_addonsReady = loaded && (_addonsReady || AreActionBarsLoaded());
#if DEBUG
					if (EventManager.Config.LogEvents && EventManager.Config.LogEventGame && EventManager.Config.LogEventGameAddonsLoaded)
					{
						Logger.Debug("AddonsLoaded", $"Loaded: {loaded} Ready: {_addonsReady}");
					}
#endif
					AddonsLoaded?.Invoke(loaded, _addonsReady);
				}
				catch (Exception ex)
				{
					Logger.Error(ex, "AddonsLoaded", $"Failed invoking {nameof(AddonsLoaded)}: {ex}");
				}
			}
		}

		private void OnConditionChange(ConditionFlag flag, bool value)
		{
			if (flag == ConditionFlag.CreatingCharacter)
			{
				SetAddonsLoaded(!value);
			}
		}

		private void OnLogin(object? sender, EventArgs e)
		{
			SetAddonsLoaded(true);
		}

		private void OnLogout(object? sender, EventArgs e)
		{
			SetAddonsLoaded(false);
		}

		public bool AreAddonsShown(bool cached = true)
		{
			if (_addonsVisibilityCached && cached)
			{
				return AreAddonsVisible;
			}

			return Plugin.ClientState.IsLoggedIn && !(Plugin.Condition[ConditionFlag.WatchingCutscene] || Plugin.Condition[ConditionFlag.WatchingCutscene78] || Plugin.Condition[ConditionFlag.OccupiedInCutSceneEvent] || Plugin.Condition[ConditionFlag.CreatingCharacter] || Plugin.Condition[ConditionFlag.BetweenAreas] || Plugin.Condition[ConditionFlag.BetweenAreas51] || Plugin.Condition[ConditionFlag.OccupiedSummoningBell] || Plugin.Condition[ConditionFlag.OccupiedInQuestEvent] || Plugin.Condition[ConditionFlag.OccupiedInEvent]);
		}

		private bool AreActionBarsLoaded()
		{
			AtkUnitBase* addon = (AtkUnitBase*) Plugin.GameGui.GetAddonByName(Addons.Names[Addon.ActionBar1], 1);
			return addon != null && addon->UldManager.LoadedState == 3 && addon->RootNode->DrawFlags == 12;
		}

		private void OnFrameworkUpdate(Framework framework)
		{
			bool addonVisibility = AreAddonsShown(false);

			if (AreAddonsLoaded && (!_addonsReady || _hudLayout != UNKNOWN_HUD_LAYOUT && !_hudLayoutReady) && Plugin.ClientState.IsLoggedIn)
			{
				// This is giga bullshit, maybe someday I'm skilled enough to fix this.
				bool addonsReady = AreActionBarsLoaded();
				if (!_addonsReady && addonsReady)
				{
					_addonsReady = addonsReady;
					SetAddonsLoaded(true, true);
				}

				if (!_hudLayoutReady && addonsReady)
				{
					_addonsReady = addonsReady;
					SetHudLayoutActivated(_hudLayout);
				}
			}

			if (AreAddonsVisible != addonVisibility)
			{
				AreAddonsVisible = addonVisibility;
				_addonsVisibilityCached = true;

				try
				{
#if DEBUG
					if (EventManager.Config.LogEvents && EventManager.Config.LogEventGame && EventManager.Config.LogEventGameAddonsVisibilityChanged)
					{
						Logger.Debug("AddonsVisibilityChanged", $"State: {addonVisibility}");
					}
#endif
					AddonsVisibilityChanged?.Invoke(addonVisibility);
				}
				catch (Exception ex)
				{
					Logger.Error(ex, "AddonsVisibilityChanged", $"Failed invoking {nameof(AddonsVisibilityChanged)}: {ex}");
				}
			}
		}

		private void SetHudLayoutActivated(uint hudLayout)
		{
			try
			{
				_hudLayoutReady = _addonsReady && AreActionBarsLoaded();
#if DEBUG
				if (EventManager.Config.LogEvents && EventManager.Config.LogEventGame && EventManager.Config.LogEventGameHudLayoutActivated)
				{
					Logger.Debug("HudLayoutActivated", $"Layout: {hudLayout} LayoutReady: {_hudLayoutReady}");
				}
#endif
				HudLayoutActivated?.Invoke(hudLayout, _hudLayoutReady);
			}
			catch (Exception ex)
			{
				Logger.Error(ex, "HudLayoutActivated", $"Failed invoking {nameof(HudLayoutActivated)}: {ex}");
			}
		}

		private uint SetHudLayoutDetour(IntPtr filePtr, uint hudLayout, byte unk0, byte unk1)
		{
			uint ret = 177749584; // 177749584 = Layout already active?

			try
			{
				ret = _setHudLayoutHook!.Original(filePtr, hudLayout, unk0, unk1);
#if DEBUG
				if (EventManager.Config.LogEvents && EventManager.Config.LogEventGame && EventManager.Config.LogEventGameHudLayoutActivated)
				{
					Logger.Debug("SetHudLayoutDetour", $"Result: {ret} Layout: {hudLayout}");
				}
#endif
			}
			catch (Exception ex)
			{
				Logger.Error(ex, "SetHudLayoutDetour", $"Failed invoking original SetHudLayout({filePtr.ToInt64():X}, {hudLayout}, {unk0}, {unk1}): {ex}");
			}

			if (ret == 0)
			{
				_hudLayout = hudLayout;
				SetHudLayoutActivated(hudLayout);
			}

			return ret;
		}
	}
}