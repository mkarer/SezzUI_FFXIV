using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using SezzUI.Configuration;
using SezzUI.Enums;
using SezzUI.Game.Events;
using SezzUI.Helper;
using SezzUI.Hooking;

namespace SezzUI.Modules.GameUI;

public class ElementHider : PluginModule, IHookAccessor
{
	List<IHookWrapper>? IHookAccessor.Hooks { get; set; }

	private readonly List<InteractableArea> _areas = new();

	/// <summary>
	///     Contains the current (assumed) visibility state of default game elements.
	/// </summary>
	private readonly Dictionary<Addon, bool> _currentVisibility = new();

	/// <summary>
	///     Contains the expected visibility state of default game elements based on mouseover state or other events.
	/// </summary>
	private readonly Dictionary<Addon, bool> _expectedVisibility = new();

	private bool _initialUpdate = true;
	private ElementHiderConfig Config => (ElementHiderConfig) _config;
#if DEBUG
	private readonly ElementHiderDebugConfig _debugConfig;
#endif

	private bool _isHudLayoutAgentVisible;
	private bool _wasHudLayoutAgentVisible;

	private static HookWrapper<ShowAgentInterfaceDelegate>? _showHudLayoutHook;
	private static HookWrapper<HideAgentInterfaceDelegate>? _hideHudLayoutHook;

	public unsafe delegate void ShowAgentInterfaceDelegate(AgentInterface* agentInterface);

	public unsafe delegate void HideAgentInterfaceDelegate(AgentInterface* agentInterface);

	public unsafe void ShowAgentInterfaceDetour(AgentInterface* agentInterface)
	{
		_showHudLayoutHook!.Original(agentInterface);
		_isHudLayoutAgentVisible = true;
	}

	public unsafe void HideAgentInterfaceDetour(AgentInterface* agentInterface)
	{
		// Hide gets called more than once!
		_hideHudLayoutHook!.Original(agentInterface);
		_isHudLayoutAgentVisible = false;
	}

	protected override void OnEnable()
	{
		_initialUpdate = true;

		if (Config.HideActionBarLock)
		{
			_expectedVisibility[Addon.ActionBarLock] = false;
		}

		EventManager.Game.AddonsLoaded += OnAddonsLoaded;
		EventManager.Game.AddonsVisibilityChanged += OnAddonsVisibilityChanged;
		EventManager.Game.HudLayoutActivated += OnHudLayoutActivated;

		_wasHudLayoutAgentVisible = false;
		_isHudLayoutAgentVisible = Services.GameGui.GetAddonByName("HudLayout") != IntPtr.Zero;

		if (EventManager.Game.IsInGame() && EventManager.Game.AreAddonsShown())
		{
			// Enabled module after logging in.
			OnAddonsVisibilityChanged(true);
		}
	}

	protected override void OnDisable()
	{
		EventManager.Game.AddonsLoaded -= OnAddonsLoaded;
		EventManager.Game.AddonsVisibilityChanged -= OnAddonsVisibilityChanged;
		EventManager.Game.HudLayoutActivated -= OnHudLayoutActivated;

		if (Config.RestoreVisibility)
		{
			// Show all addons
			UpdateAddons(_expectedVisibility, EventManager.Game.AreAddonsShown());
		}
		else
		{
			// Show action bar lock
			UpdateAddons(new() {{Addon.ActionBarLock, true}});
		}

		_expectedVisibility.Clear();
		_currentVisibility.Clear();
	}

	private void OnHudLayoutActivated(uint hudLayout, bool ready)
	{
		if (Services.ClientState.IsLoggedIn && !_initialUpdate && !_isHudLayoutAgentVisible && ready)
		{
			// Force update after switching layouts!
#if DEBUG
			if (_debugConfig.LogAddonsEventHandling)
			{
				Logger.Debug("Forcing update after switching layout...");
			}
#endif
			UpdateAddons(_expectedVisibility);
		}
	}

	private void OnAddonsVisibilityChanged(bool visible)
	{
		if (!Services.ClientState.IsLoggedIn || !_initialUpdate)
		{
			return;
		}

#if DEBUG
		if (_debugConfig.LogAddonsEventHandling)
		{
			Logger.Debug($"Visibility: {visible} InitialUpdate: {_initialUpdate}");
		}
#endif

		if (_initialUpdate)
		{
			// Initial update, expect incorrect visibility on all addons to force update
#if DEBUG
			if (_debugConfig.LogAddonsEventHandling)
			{
				Logger.Debug("Resetting cached visibility states.");
			}
#endif
			foreach (KeyValuePair<Addon, bool> expected in _expectedVisibility)
			{
				_currentVisibility[expected.Key] = !expected.Value;
			}
		}

		if (!visible)
		{
			// Hide all, ignore expected states
#if DEBUG
			if (_debugConfig.LogAddonsEventHandling)
			{
				Logger.Debug("Hiding all addons, ignoring expected states.");
			}
#endif
			UpdateAddons(_expectedVisibility, false);
		}
		else
		{
			// Toggle visibility based on state
#if DEBUG
			if (_debugConfig.LogAddonsEventHandling)
			{
				Logger.Debug("Updating visibility based on expected states.");
			}
#endif
			Dictionary<Addon, bool>? update = null;

			foreach (KeyValuePair<Addon, bool> expected in _expectedVisibility)
			{
				if (expected.Value != _currentVisibility[expected.Key])
				{
					update ??= new();
#if DEBUG
					if (_debugConfig.LogAddonsEventHandling || _debugConfig.LogVisibilityStates || _debugConfig.LogVisibilityStatesVerbose)
					{
						Logger.Debug($"Addon needs update: {expected.Key} (Current: {_currentVisibility[expected.Key]} Expected: {expected.Value})");
					}
#endif
					update[expected.Key] = expected.Value;
				}
			}

			if (update != null)
			{
				UpdateAddons(update);
			}
		}

		_initialUpdate = false;
	}

	private void OnAddonsLoaded(bool loaded, bool ready)
	{
#if DEBUG
		if (_debugConfig.LogAddonsEventHandling)
		{
			Logger.Debug($"Loaded: {loaded} Ready: {ready}");
		}
#endif
		if (loaded)
		{
			// Force update after visiting the Aesthetician!
#if DEBUG
			if (_debugConfig.LogAddonsEventHandling)
			{
				Logger.Debug("Forcing initial update...");
			}
#endif
			_initialUpdate = true;
			OnAddonsVisibilityChanged(EventManager.Game.AreAddonsShown()); // TODO: Test
		}
	}

	protected override void OnDraw(DrawState drawState)
	{
		if (drawState != DrawState.Visible && drawState != DrawState.Partially)
		{
			return;
		}

		// Hide gets called more than once...
		if (_wasHudLayoutAgentVisible != _isHudLayoutAgentVisible)
		{
			_wasHudLayoutAgentVisible = _isHudLayoutAgentVisible;
#if DEBUG
			if (_debugConfig.LogGeneral)
			{
				Logger.Debug($"isEditingHudLayouts: {_isHudLayoutAgentVisible} -> {(_isHudLayoutAgentVisible ? "Show all addons!" : "Update!")}");
			}
#endif
			if (_isHudLayoutAgentVisible)
			{
				// Show all
				UpdateAddons(_expectedVisibility, true);
			}
			else
			{
				// Done, force update
				UpdateAddons(_expectedVisibility, true);
			}
		}

		if (_isHudLayoutAgentVisible)
		{
			return;
		}

		bool updateNeeded = false;

		foreach (InteractableArea area in _areas)
		{
			if (area.Config.Enabled)
			{
				area.Draw();
				foreach (int addonId in area.Config.Elements)
				{
					if (!Enum.IsDefined(typeof(Addon), addonId))
					{
						continue;
					}

					Addon element = (Addon) addonId;
					_expectedVisibility[element] = area.IsHovered && EventManager.Game.AreAddonsShown(); // TODO: AreAddonsShown should check if the elements used in that area could be shown.
#if DEBUG
					if (_debugConfig.LogVisibilityUpdates && (!_currentVisibility.ContainsKey(element) || _currentVisibility[element] != _expectedVisibility[element]))
					{
						Logger.Debug($"Addon needs update: {element} (Current: {(_currentVisibility.ContainsKey(element) ? _currentVisibility[element] : "Unknown")} Expected: {_expectedVisibility[element]})");
					}
#endif
					updateNeeded |= !_currentVisibility.ContainsKey(element) || _currentVisibility[element] != _expectedVisibility[element];
				}
			}
		}

		if (updateNeeded)
		{
#if DEBUG
			if (_debugConfig.LogVisibilityUpdates)
			{
				Logger.Debug("Updating addons...");
			}
#endif
			UpdateAddons(_expectedVisibility);
		}
	}

	#region Addons

#if DEBUG
	private static string ToBinaryString(byte number) => Convert.ToString(number, 2).PadLeft(8, '0');
#endif

	private unsafe void UpdateAddonVisibility(Addon element, IntPtr addon, bool shouldShow, bool isNode = false)
	{
		_currentVisibility[element] = shouldShow; // Assume the update went as expected...

		if (!isNode)
		{
			// AtkUnitBase
			byte visibilityFlag = ((AtkUnitBase*) addon)->VisibilityFlags;
			byte expectedVisibilityFlag = (byte) (shouldShow ? visibilityFlag & ~(byte) AddonVisibility.UserHidden : visibilityFlag | (byte) AddonVisibility.UserHidden);

			if (visibilityFlag != expectedVisibilityFlag)
			{
#if DEBUG
				if (_debugConfig.LogVisibilityUpdates)
				{
					Logger.Debug($"Addon: {element} ShouldShow: {shouldShow} visibilityFlag: {ToBinaryString(visibilityFlag)} -> {ToBinaryString(expectedVisibilityFlag)}");
				}
#endif
				((AtkUnitBase*) addon)->VisibilityFlags = expectedVisibilityFlag;
			}
		}
		else
		{
			// AtkResNode
			AtkResNode* node = (AtkResNode*) addon;
			if (shouldShow != node->IsVisible())
			{
#if DEBUG
				if (_debugConfig.LogVisibilityUpdates)
				{
					Logger.Debug($"Addon: {element} ShouldShow: {shouldShow} IsVisible: {node->IsVisible()}");
				}
#endif
				node->ToggleVisibility(shouldShow);
			}
		}
	}

	private unsafe void UpdateAddons(Dictionary<Addon, bool> elements, bool? forcedVisibility = null)
	{
#if DEBUG
		if (_debugConfig.LogVisibilityStatesVerbose)
		{
			Logger.Debug($"Watched Addons: {elements.Count} ForcedVisibility: {forcedVisibility}");
		}
#endif

		AtkStage* stage = AtkStage.Instance();
		if (stage == null)
		{
			return;
		}

		AtkUnitList* loadedUnitsList = &stage->RaptureAtkUnitManager->AtkUnitManager.AllLoadedUnitsList;

#if DEBUG
		if (_debugConfig.LogVisibilityStatesVerbose)
		{
			foreach ((Addon k, bool v) in _expectedVisibility)
			{
				Logger.Debug($"Addon: {k} ExpectedVisibility: {v} UpdatedVisibility {forcedVisibility ?? v}");
			}
		}
#endif

		for (int i = 0; i < loadedUnitsList->Count; i++)
		{
			AtkUnitBase* addon = *(AtkUnitBase**) Unsafe.AsPointer(ref loadedUnitsList->Entries[i]);
			if (addon == null || addon->RootNode == null || addon->UldManager.LoadedState != AtkLoadState.Loaded)
			{
				continue;
			}

			string? name = addon->NameString;
			if (name == null)
			{
				continue;
			}

			foreach ((Addon element, bool expectedVisibility) in elements)
			{
				bool shouldShow = forcedVisibility ?? expectedVisibility;

				if (Addons.Names.TryGetValue(element, out string? value))
				{
					if (name == value)
					{
						UpdateAddonVisibility(element, (IntPtr) addon, shouldShow);
					}
				}
				else
				{
					switch (element)
					{
						case Addon.ActionBarLock:
							// The lock is a CheckBox type child node of _ActionBar!
							if (name == "_ActionBar")
							{
								if (addon->UldManager.NodeListCount > 0)
								{
									for (int j = 0; j < addon->UldManager.NodeListCount; j++)
									{
										AtkResNode* node = addon->UldManager.NodeList[j];
										if (node != null && (int) node->Type >= 1000)
										{
											AtkComponentNode* compNode = (AtkComponentNode*) node;
											AtkUldComponentInfo* objectInfo = (AtkUldComponentInfo*) compNode->Component->UldManager.Objects;

											if (objectInfo->ComponentType == ComponentType.CheckBox)
											{
												// This should be the lock!
												UpdateAddonVisibility(element, (IntPtr) node, shouldShow, true);
												break;
											}
										}
									}
								}
							}

							break;

						case Addon.Job:
							if (name.StartsWith("JobHud"))
							{
								UpdateAddonVisibility(element, (IntPtr) addon, shouldShow);
							}

							break;

						case Addon.Chat:
							if (name.StartsWith("ChatLog"))
							{
								UpdateAddonVisibility(element, (IntPtr) addon, shouldShow);
							}

							break;

						case Addon.TargetInfo:
							if (name.StartsWith("TargetInfo"))
							{
								UpdateAddonVisibility(element, (IntPtr) addon, shouldShow);
							}

							break;

						case Addon.CrossHotbar:
							if (name.StartsWith("Action") && name.Contains("Cross"))
							{
								UpdateAddonVisibility(element, (IntPtr) addon, shouldShow);
							}

							break;

						default:
							Logger.Error($"Unsupported UI Element: {element}");
							break;
					}
				}
			}
		}
	}

	#endregion

	public unsafe ElementHider(ElementHiderConfig config) : base(config)
	{
#if DEBUG
		_debugConfig = Singletons.Get<ConfigurationManager>().GetConfigObject<ElementHiderDebugConfig>();
#endif

		try
		{
			AgentInterface* agentHudLayout = Framework.Instance()->GetUIModule()->GetAgentModule()->GetAgentByInternalId(AgentId.HudLayout);
			_showHudLayoutHook = (this as IHookAccessor).Hook<ShowAgentInterfaceDelegate>(agentHudLayout->VirtualTable->Show, ShowAgentInterfaceDetour);
			_hideHudLayoutHook = (this as IHookAccessor).Hook<HideAgentInterfaceDelegate>(agentHudLayout->VirtualTable->Hide, HideAgentInterfaceDetour);
		}
		catch (Exception ex)
		{
			Logger.Error($"Failed to setup hooks: {ex}");
		}

		foreach (InteractableAreaConfig areaConfig in Config.Areas)
		{
			_areas.Add(new(areaConfig));
			areaConfig.ValueChangeEvent += OnConfigPropertyChanged;
		}

		DraggableElements.AddRange(_areas);

		Config.ValueChangeEvent += OnConfigPropertyChanged;
		Singletons.Get<ConfigurationManager>().Reset += OnConfigReset;
		(this as IPluginComponent).SetEnabledState(Config.Enabled);
	}

	protected override void OnDispose()
	{
		Config.ValueChangeEvent -= OnConfigPropertyChanged;
		Config.Areas.ForEach(x => x.ValueChangeEvent -= OnConfigPropertyChanged);
		Singletons.Get<ConfigurationManager>().Reset -= OnConfigReset;
		_areas.Clear();
	}

	#region Configuration Events

	private void OnConfigPropertyChanged(object sender, OnChangeBaseArgs args)
	{
		if (sender is InteractableAreaConfig)
		{
			if (args.PropertyName == "Enabled" || args.PropertyName == "Elements")
			{
#if DEBUG
				if (_debugConfig.LogConfigurationManager)
				{
					Logger.Debug($"{args.PropertyName}: Resetting expected visibility states...");
				}
#endif
				foreach (Addon element in _expectedVisibility.Keys.Where(x => x != Addon.ActionBarLock)) // ActionBarLock is not shown on mouseover!
				{
					_expectedVisibility[element] = true;
				}

				UpdateAddons(_expectedVisibility);
			}

			return;
		}

		switch (args.PropertyName)
		{
			case "Enabled":
#if DEBUG
				if (_debugConfig.LogConfigurationManager)
				{
					Logger.Debug($"{args.PropertyName}: {Config.Enabled}");
				}
#endif
				(this as IPluginComponent).SetEnabledState(Config.Enabled);
				break;

			case "HideActionBarLock":
#if DEBUG
				if (_debugConfig.LogConfigurationManager)
				{
					Logger.Debug($"{args.PropertyName}: {Config.HideActionBarLock}");
				}
#endif
				if (Config.Enabled)
				{
					_expectedVisibility[Addon.ActionBarLock] = !Config.HideActionBarLock;
					UpdateAddons(new() {{Addon.ActionBarLock, _expectedVisibility[Addon.ActionBarLock]}});
				}

				break;
		}
	}

	private void OnConfigReset(ConfigurationManager sender, PluginConfigObject config)
	{
		if (config != _config)
		{
			return;
		}
#if DEBUG
		if (_debugConfig.LogConfigurationManager)
		{
			Logger.Debug("Resetting...");
		}
#endif
		(this as IPluginComponent).Disable();
#if DEBUG
		if (_debugConfig.LogConfigurationManager)
		{
			Logger.Debug($"Config.Enabled: {Config.Enabled}");
		}
#endif
		(this as IPluginComponent).SetEnabledState(Config.Enabled);
	}

	#endregion
}