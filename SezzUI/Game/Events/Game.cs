using System;
using System.Collections.Generic;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Component.GUI;
using SezzUI.Hooking;
using SezzUI.Modules;
using SezzUI.Modules.GameUI;

namespace SezzUI.Game.Events;

internal sealed unsafe class Game : BaseEvent, IHookAccessor
{
	List<IHookWrapper>? IHookAccessor.Hooks { get; set; }

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

	private readonly HookWrapper<SetHudLayoutDelegate>? _setHudLayoutHook;

	private bool _hudLayoutReady;
	private static readonly uint UNKNOWN_HUD_LAYOUT = 10;
	private uint _hudLayout = UNKNOWN_HUD_LAYOUT;

	public Game()
	{
		_setHudLayoutHook = (this as IHookAccessor).Hook<SetHudLayoutDelegate>("E8 ?? ?? ?? ?? 33 C0 EB 12", SetHudLayoutDetour, 0, true); // https://github.com/zacharied/FFXIV-Plugin-HudManager/blob/testing/HUDManager/Hud.cs

		(this as IPluginComponent).Enable();
	}

	protected override void OnEnable()
	{
		Services.Condition.ConditionChange += OnConditionChange;
		Services.Framework.Update += OnFrameworkUpdate;
		Services.ClientState.Login += OnLogin;
		Services.ClientState.Logout += OnLogout;

		if (IsInGame())
		{
			SetAddonsLoaded(true);
		}
	}

	protected override void OnDisable()
	{
		Services.Condition.ConditionChange -= OnConditionChange;
		Services.Framework.Update -= OnFrameworkUpdate;
		Services.ClientState.Login -= OnLogin;
		Services.ClientState.Logout -= OnLogout;

		AreAddonsLoaded = false;
		_addonsReady = false;
		AreAddonsVisible = false;
		_addonsVisibilityCached = false;
		_hudLayoutReady = false;
		_hudLayout = UNKNOWN_HUD_LAYOUT;
	}

	/// <summary>
	///     Player is in game and addons are loaded.
	/// </summary>
	/// <returns></returns>
	public bool IsInGame() => Services.ClientState.IsLoggedIn && !Services.Condition[ConditionFlag.CreatingCharacter];

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
				if (Plugin.DebugConfig.LogEvents && Plugin.DebugConfig.LogEventGame && Plugin.DebugConfig.LogEventGameAddonsLoaded)
				{
					Logger.Debug($"Loaded: {loaded} Ready: {_addonsReady}");
				}
#endif
				AddonsLoaded?.Invoke(loaded, _addonsReady);
			}
			catch (Exception ex)
			{
				Logger.Error($"Failed invoking {nameof(AddonsLoaded)}: {ex}");
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

	private void OnLogin()
	{
		SetAddonsLoaded(true);
	}

	private void OnLogout(int type, int code)
	{
		SetAddonsLoaded(false);
	}

	public bool AreAddonsShown(bool cached = true)
	{
		if (_addonsVisibilityCached && cached)
		{
			return AreAddonsVisible;
		}

		return Services.ClientState.IsLoggedIn && !(Services.Condition[ConditionFlag.WatchingCutscene] || Services.Condition[ConditionFlag.WatchingCutscene78] || Services.Condition[ConditionFlag.OccupiedInCutSceneEvent] || Services.Condition[ConditionFlag.CreatingCharacter] || Services.Condition[ConditionFlag.BetweenAreas] || Services.Condition[ConditionFlag.BetweenAreas51] || Services.Condition[ConditionFlag.OccupiedSummoningBell] || Services.Condition[ConditionFlag.OccupiedInQuestEvent] || Services.Condition[ConditionFlag.OccupiedInEvent]);
	}

	private bool AreActionBarsLoaded()
	{
		AtkUnitBase* addon = (AtkUnitBase*) Services.GameGui.GetAddonByName(Addons.Names[Addon.ActionBar1]).Address;
		return (IntPtr) addon != IntPtr.Zero && addon->UldManager.LoadedState == AtkLoadState.Loaded && addon->RootNode->DrawFlags == 12;
	}

	private void OnFrameworkUpdate(IFramework framework)
	{
		bool addonVisibility = AreAddonsShown(false);

		if (AreAddonsLoaded && (!_addonsReady || (_hudLayout != UNKNOWN_HUD_LAYOUT && !_hudLayoutReady)) && Services.ClientState.IsLoggedIn)
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
				if (Plugin.DebugConfig.LogEvents && Plugin.DebugConfig.LogEventGame && Plugin.DebugConfig.LogEventGameAddonsVisibilityChanged)
				{
					Logger.Debug($"AddonsVisibilityChanged: {addonVisibility}");
				}
#endif
				AddonsVisibilityChanged?.Invoke(addonVisibility);
			}
			catch (Exception ex)
			{
				Logger.Error($"Failed invoking {nameof(AddonsVisibilityChanged)}: {ex}");
			}
		}
	}

	private void SetHudLayoutActivated(uint hudLayout)
	{
		try
		{
			_hudLayoutReady = _addonsReady && AreActionBarsLoaded();
#if DEBUG
			if (Plugin.DebugConfig.LogEvents && Plugin.DebugConfig.LogEventGame && Plugin.DebugConfig.LogEventGameHudLayoutActivated)
			{
				Logger.Debug($"Layout: {hudLayout} LayoutReady: {_hudLayoutReady}");
			}
#endif
			HudLayoutActivated?.Invoke(hudLayout, _hudLayoutReady);
		}
		catch (Exception ex)
		{
			Logger.Error($"Failed invoking {nameof(HudLayoutActivated)}: {ex}");
		}
	}

	private uint SetHudLayoutDetour(IntPtr filePtr, uint hudLayout, byte unk0, byte unk1)
	{
		uint ret = 177749584; // 177749584 = Layout already active?

		try
		{
			ret = _setHudLayoutHook!.Original(filePtr, hudLayout, unk0, unk1);
#if DEBUG
			if (Plugin.DebugConfig.LogEvents && Plugin.DebugConfig.LogEventGame && Plugin.DebugConfig.LogEventGameHudLayoutActivated)
			{
				Logger.Debug($"Result: {ret} Layout: {hudLayout}");
			}
#endif
		}
		catch (Exception ex)
		{
			Logger.Error($"Failed invoking original SetHudLayout({filePtr.ToInt64():X}, {hudLayout}, {unk0}, {unk1}): {ex}");
		}

		if (ret == 0)
		{
			_hudLayout = hudLayout;
			SetHudLayoutActivated(hudLayout);
		}

		return ret;
	}
}