using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using FFXIVClientStructs.FFXIV.Component.GUI;
using SezzUI.Config;
using SezzUI.Enums;
using SezzUI.GameEvents;
using SezzUI.Interface.GeneralElements;

namespace SezzUI.Modules.GameUI
{
	public class ElementHider : PluginModule
	{
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

		protected override bool Enable()
		{
			if (!base.Enable())
			{
				return false;
			}

			_initialUpdate = true;

			if (Config.HideActionBarLock)
			{
				_expectedVisibility[Addon.ActionBarLock] = false;
			}

			EventManager.Game.AddonsLoaded += OnAddonsLoaded;
			EventManager.Game.AddonsVisibilityChanged += OnAddonsVisibilityChanged;

			if (Game.Instance.IsInGame() && EventManager.Game.AreAddonsShown())
			{
				// Enabled module after logging in.
				OnAddonsVisibilityChanged(true);
			}
			return true;
		}

		protected override bool Disable()
		{
			if (!base.Disable())
			{
				return false;
			}

			EventManager.Game.AddonsLoaded -= OnAddonsLoaded;
			EventManager.Game.AddonsVisibilityChanged -= OnAddonsVisibilityChanged;
			UpdateAddons(_expectedVisibility, EventManager.Game.AreAddonsShown());
			_expectedVisibility.Clear();
			_currentVisibility.Clear();

			return true;
		}

		private void OnAddonsVisibilityChanged(bool visible)
		{
			if (!Plugin.ClientState.IsLoggedIn || !_initialUpdate)
			{
				return;
			}

#if DEBUG
			if (_debugConfig.LogAddonsEventHandling)
			{
				Logger.Debug("OnAddonsVisibilityChanged", $"Visibility: {visible} InitialUpdate: {_initialUpdate}");
			}
#endif

			if (_initialUpdate)
			{
				// Initial update, expect incorrect visibility on all addons to force update
#if DEBUG
				if (_debugConfig.LogAddonsEventHandling)
				{
					Logger.Debug("OnAddonsVisibilityChanged", "Resetting cached visibility states.");
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
					Logger.Debug("OnAddonsVisibilityChanged", "Hiding all addons, ignoring expected states.");
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
					Logger.Debug("OnAddonsVisibilityChanged", "Updating visibility based on expected states.");
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
							Logger.Debug("OnAddonsVisibilityChanged", $"Addon needs update: {expected.Key} (Current: {_currentVisibility[expected.Key]} Expected: {expected.Value})");
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
				Logger.Debug("OnAddonsLoaded", $"Loaded: {loaded} Ready: {ready}");
			}
#endif
			if (loaded)
			{
				// Force update after visiting the Aesthetician!
#if DEBUG
				if (_debugConfig.LogAddonsEventHandling)
				{
					Logger.Debug("OnAddonsLoaded", "Forcing initial update...");
				}
#endif
				_initialUpdate = true;
				OnAddonsVisibilityChanged(EventManager.Game.AreAddonsShown()); // TODO: Test
			}
		}

		public override void Draw(DrawState drawState, Vector2? origin)
		{
			if (!Enabled || drawState != DrawState.Visible)
			{
				return;
			}

			bool updateNeeded = false;

			foreach (InteractableArea area in _areas)
			{
				if (area.Config.Enabled)
				{
					area.DrawChildren((Vector2) origin!);
					foreach (int addonId in area.Config.Elements)
					{
						if (!Enum.IsDefined(typeof(Addon), addonId))
						{
							continue;
						}

						Addon element = (Addon) addonId;

						// TODO: Some addons (MainMenu) are shown while others are not, AreAddonsShown() needs to be changed.
						_expectedVisibility[element] = area.IsHovered && EventManager.Game.AreAddonsShown(); // TODO: AtkEvent: MouseOver, MouseOut
#if DEBUG
						if (_debugConfig.LogVisibilityUpdates && (!_currentVisibility.ContainsKey(element) || _currentVisibility[element] != _expectedVisibility[element]))
						{
							Logger.Debug("Draw", $"Addon needs update: {element} (Current: {(_currentVisibility.ContainsKey(element) ? _currentVisibility[element] : "Unknown")} Expected: {_expectedVisibility[element]})");
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
					Logger.Debug("Draw", "Updating addons...");
				}
#endif
				UpdateAddons(_expectedVisibility);
			}
		}

		#region Addons

		private unsafe void UpdateAddonVisibility(Addon element, IntPtr addon, bool shouldShow, bool modifyNodeList = true, bool isRootNode = false)
		{
			_currentVisibility[element] = shouldShow; // Assume the update went as expected...
			AtkResNode* rootNode = isRootNode ? (AtkResNode*) addon : ((AtkUnitBase*) addon)->RootNode;
			modifyNodeList &= !isRootNode;

			// This hides them from the HUD layout manager aswell and showing doesn't work on the Scenario Guide.
			// Also it makes them fade out and in (although not really smooth).
			//if (shouldShow)
			//{
			//    addon->Show(0, false);
			//}
			//else
			//{
			//    addon->Hide(false);
			//}

			// This seems fine but doesn't trigger MouseOut I guess.
			if (shouldShow != rootNode->IsVisible)
			{
#if DEBUG
				if (_debugConfig.LogVisibilityUpdates && shouldShow != rootNode->IsVisible)
				{
					Logger.Debug("UpdateAddonVisibility", $"Addon: {element} ShouldShow: {shouldShow} IsVisible: {rootNode->IsVisible}");
				}
#endif
				rootNode->Flags ^= 0x10;
			}

			if (modifyNodeList)
			{
				AtkUnitBase* addonUnitBase = (AtkUnitBase*) addon;
				if (addonUnitBase->RootNode->IsVisible && addonUnitBase->UldManager.NodeListCount == 0)
				{
#if DEBUG
					if (_debugConfig.LogVisibilityUpdates)
					{
						Logger.Debug("UpdateAddonVisibility", $"Addon: {element} DrawNodeList -> Update");
					}
#endif
					addonUnitBase->UldManager.UpdateDrawNodeList();
				}
				else if (!addonUnitBase->RootNode->IsVisible && addonUnitBase->UldManager.NodeListCount != 0)
				{
#if DEBUG
					if (_debugConfig.LogVisibilityUpdates)
					{
						Logger.Debug("UpdateAddonVisibility", $"Addon: {element} NodeListCount -> 0");
					}
#endif
					addonUnitBase->UldManager.NodeListCount = 0;
				}
			}
		}

		private unsafe void UpdateAddons(Dictionary<Addon, bool> elements, bool? forcedVisibility = null)
		{
#if DEBUG
			if (_debugConfig.LogVisibilityStatesVerbose)
			{
				Logger.Debug("UpdateAddons", $"Watched Addons: {elements.Count} ForcedVisibility: {forcedVisibility}");
			}
#endif

			AtkStage* stage = AtkStage.GetSingleton();
			if (stage == null)
			{
				return;
			}

			AtkUnitList* loadedUnitsList = &stage->RaptureAtkUnitManager->AtkUnitManager.AllLoadedUnitsList;
			if (loadedUnitsList == null)
			{
				return;
			}

			AtkUnitBase** addonList = &loadedUnitsList->AtkUnitEntries;
			if (addonList == null)
			{
				return;
			}

#if DEBUG
			if (_debugConfig.LogVisibilityStatesVerbose)
			{
				foreach ((Addon k, bool v) in _expectedVisibility)
				{
					Logger.Debug("UpdateAddons", $"Addon: {k} ExpectedVisibility: {v} UpdatedVisibility {forcedVisibility ?? v}");
				}
			}
#endif

			for (int i = 0; i < loadedUnitsList->Count; i++)
			{
				AtkUnitBase* addon = addonList[i];
				if (addon == null || addon->RootNode == null || addon->UldManager.LoadedState != 3)
				{
					continue;
				}

				string? name = Marshal.PtrToStringAnsi(new(addon->Name));
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
													UpdateAddonVisibility(element, (IntPtr) node, shouldShow, false, true);
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
								Logger.Error("UpdateAddons", $"Unsupported UI Element: {element}");
								break;
						}
					}
				}
			}
		}

		#endregion

		#region Singleton

		private ElementHider(ElementHiderConfig config) : base(config)
		{
#if DEBUG
			_debugConfig = ConfigurationManager.Instance.GetConfigObject<ElementHiderDebugConfig>();
#endif
			foreach (InteractableAreaConfig areaConfig in Config.Areas)
			{
				_areas.Add(new(areaConfig));
				areaConfig.ValueChangeEvent += OnConfigPropertyChanged;
			}

			DraggableElements.AddRange(_areas);

			Config.ValueChangeEvent += OnConfigPropertyChanged;
			ConfigurationManager.Instance.Reset += OnConfigReset;
			Toggle(Config.Enabled);
		}

		public static void Initialize()
		{
			Instance = new(ConfigurationManager.Instance.GetConfigObject<ElementHiderConfig>());
		}

		public static ElementHider Instance { get; private set; } = null!;

		protected override void InternalDispose()
		{
			Config.ValueChangeEvent -= OnConfigPropertyChanged;
			Config.Areas.ForEach(x => x.ValueChangeEvent -= OnConfigPropertyChanged);
			ConfigurationManager.Instance.Reset -= OnConfigReset;
			_areas.Clear();
		}

		~ElementHider()
		{
			Dispose(false);
		}

		#endregion

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
						Logger.Debug("OnConfigPropertyChanged", $"{args.PropertyName}: Resetting expected visibility states...");
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
						Logger.Debug("OnConfigPropertyChanged", $"{args.PropertyName}: {Config.Enabled}");
					}
#endif
					Toggle(Config.Enabled);
					break;

				case "HideActionBarLock":
#if DEBUG
					if (_debugConfig.LogConfigurationManager)
					{
						Logger.Debug("OnConfigPropertyChanged", $"{args.PropertyName}: {Config.HideActionBarLock}");
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
			if (config is not ElementHiderConfig)
			{
				return;
			}
#if DEBUG
			if (_debugConfig.LogConfigurationManager)
			{
				Logger.Debug("OnConfigReset", "Resetting...");
			}
#endif
			Disable();
#if DEBUG
			if (_debugConfig.LogConfigurationManager)
			{
				Logger.Debug("OnConfigReset", $"Config.Enabled: {Config.Enabled}");
			}
#endif
			Toggle(Config.Enabled);
		}

		#endregion
	}
}