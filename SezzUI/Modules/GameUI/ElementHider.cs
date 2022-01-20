using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;
using SezzUI.Config;
using SezzUI.Enums;
using SezzUI.Interface.GeneralElements;

namespace SezzUI.Modules.GameUI
{
	public class ElementHider : HudModule
	{
		private readonly List<InteractableArea> _areas = new();

		/// <summary>
		///     Contains the current (assumed) visibility state of default game elements.
		/// </summary>
		private readonly Dictionary<Element, bool> _currentVisibility = new();

		/// <summary>
		///     Contains the expected visibility state of default game elements based on mouseover state or other events.
		/// </summary>
		private readonly Dictionary<Element, bool> _expectedVisibility = new();

		private bool _initialUpdate = true;
		private ElementHiderConfig Config => (ElementHiderConfig) _config;
#if DEBUG
		private ElementHiderDebugConfig _debugConfig;
#endif

		protected override bool Enable()
		{
			if (!base.Enable())
			{
				return false;
			}

			_initialUpdate = true;

			// TEMPORARY CONFIGURATION
			// TODO: Add to configuration UI with placeholders
			using (InteractableArea area = new(new()))
			{
				area.Elements.AddRange(new List<Element> {Element.MainMenu, Element.ActionBar4});
				area.Position = new(4, ImGui.GetMainViewport().Size.Y - 4);
				area.Anchor = DrawAnchor.BottomLeft;
				area.Size = new(780, 50); // TODO: Automatic sizing ? Node.Width * Node.ScaleX, Node.Height * Node.ScaleY
				_areas.Add(area);
			}

			using (InteractableArea area = new(new()))
			{
				area.Elements.AddRange(new List<Element> {Element.ActionBar5, Element.ActionBar10});
				area.Position = new(ImGui.GetMainViewport().Size.X / 2, ImGui.GetMainViewport().Size.Y - 4);
				area.Anchor = DrawAnchor.Bottom;
				area.Size = new(500, 90);
				_areas.Add(area);
			}

			using (InteractableArea area = new(new()))
			{
				area.Elements.AddRange(new List<Element> {Element.ActionBar7, Element.ActionBar8, Element.ActionBar9});
				area.Position = new(ImGui.GetMainViewport().Size.X - 4, 710);
				area.Anchor = DrawAnchor.Right;
				area.Size = new(176, 670);
				_areas.Add(area);
			}

			using (InteractableArea area = new(new()))
			{
				area.Elements.AddRange(new List<Element> {Element.ScenarioGuide});
				area.Position = new(ImGui.GetMainViewport().Size.X - 4, 0);
				area.Anchor = DrawAnchor.TopRight;
				area.Size = new(340, 670);
				area.Size = new(340, 100);
				_areas.Add(area);
			}

			if (Config.HideActionBarLock)
			{
				_expectedVisibility[Element.ActionBarLock] = false;
			}

			EventManager.Game.AddonsLoaded += OnAddonsLoaded;
			EventManager.Game.AddonsVisibilityChanged += OnAddonsVisibilityChanged;
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
			_areas.Clear();
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
				LogDebug("OnAddonsVisibilityChanged", $"Visibility: {visible} InitialUpdate: {_initialUpdate}");
			}
#endif

			if (_initialUpdate)
			{
				// Initial update, expect incorrect visibility on all addons to force update
#if DEBUG
				if (_debugConfig.LogAddonsEventHandling)
				{
					LogDebug("OnAddonsVisibilityChanged", "Resetting cached visibility states.");
				}
#endif
				foreach (KeyValuePair<Element, bool> expected in _expectedVisibility)
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
					LogDebug("OnAddonsVisibilityChanged", "Hiding all addons, ignoring expected states.");
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
					LogDebug("OnAddonsVisibilityChanged", "Updating visibility based on expected states.");
				}
#endif
				Dictionary<Element, bool>? update = null;

				foreach (KeyValuePair<Element, bool> expected in _expectedVisibility)
				{
					if (expected.Value != _currentVisibility[expected.Key])
					{
						update ??= new();
#if DEBUG
						if (_debugConfig.LogAddonsEventHandling || _debugConfig.LogVisibilityStates || _debugConfig.LogVisibilityStatesVerbose)
						{
							LogDebug("OnAddonsVisibilityChanged", $"Addon needs update: {expected.Key} (Current: {_currentVisibility[expected.Key]} Expected: {expected.Value})");
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
				LogDebug("OnAddonsLoaded", $"Loaded: {loaded} Ready: {ready}");
			}
#endif
			if (loaded)
			{
				// Force update after visiting the Aesthetician!
#if DEBUG
				if (_debugConfig.LogAddonsEventHandling)
				{
					LogDebug("OnAddonsLoaded", "Forcing initial update...");
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
				if (area.Enabled)
				{
					area.Draw();
					foreach (Element element in area.Elements)
					{
						// TODO: Some addons (MainMenu) are shown while others are not, AreAddonsShown() needs to be changed.
						_expectedVisibility[element] = area.IsHovered && EventManager.Game.AreAddonsShown(); // TOOD: AtkEvent: MouseOver, MouseOut
#if DEBUG
						if (_debugConfig.LogVisibilityUpdates && (!_currentVisibility.ContainsKey(element) || _currentVisibility[element] != _expectedVisibility[element]))
						{
							LogDebug("Draw", $"Addon needs update: {element} (Current: {(_currentVisibility.ContainsKey(element) ? _currentVisibility[element] : "Unknown")} Expected: {_expectedVisibility[element]})");
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
					LogDebug("Draw", "Updating addons...");
				}
#endif
				UpdateAddons(_expectedVisibility);
			}
		}

		#region Addons

		private unsafe void UpdateAddonVisibility(Element element, IntPtr addon, bool shouldShow, bool modifyNodeList = true, bool isRootNode = false)
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
					LogDebug("UpdateAddonVisibility", $"Addon: {element} ShouldShow: {shouldShow} IsVisible: {rootNode->IsVisible}");
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
						LogDebug("UpdateAddonVisibility", $"Addon: {element} DrawNodeList -> Update");
					}
#endif
					addonUnitBase->UldManager.UpdateDrawNodeList();
				}
				else if (!addonUnitBase->RootNode->IsVisible && addonUnitBase->UldManager.NodeListCount != 0)
				{
#if DEBUG
					if (_debugConfig.LogVisibilityUpdates)
					{
						LogDebug("UpdateAddonVisibility", $"Addon: {element} NodeListCount -> 0");
					}
#endif
					addonUnitBase->UldManager.NodeListCount = 0;
				}
			}
		}

		private unsafe void UpdateAddons(Dictionary<Element, bool> elements, bool? forcedVisibility = null)
		{
#if DEBUG
			if (_debugConfig.LogVisibilityStatesVerbose)
			{
				LogDebug("UpdateAddons", $"Watched Addons: {elements.Count} ForcedVisibility: {forcedVisibility}");
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
				foreach ((Element k, bool v) in _expectedVisibility)
				{
					LogDebug("UpdateAddons", $"Addon: {k} ExpectedVisibility: {v} UpdatedVisibility {forcedVisibility ?? v}");
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

				foreach ((Element element, bool expectedVisibility) in elements)
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
							case Element.ActionBarLock:
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

							case Element.Job:
								if (name.StartsWith("JobHud"))
								{
									UpdateAddonVisibility(element, (IntPtr) addon, shouldShow);
								}

								break;

							case Element.Chat:
								if (name.StartsWith("ChatLog"))
								{
									UpdateAddonVisibility(element, (IntPtr) addon, shouldShow);
								}

								break;

							case Element.TargetInfo:
								if (name.StartsWith("TargetInfo"))
								{
									UpdateAddonVisibility(element, (IntPtr) addon, shouldShow);
								}

								break;

							case Element.CrossHotbar:
								if (name.StartsWith("Action") && name.Contains("Cross"))
								{
									{
										UpdateAddonVisibility(element, (IntPtr) addon, shouldShow);
									}
								}

								break;

							default:
								LogError("UpdateAddons", $"Unsupported UI Element: {element}");
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
			config.ValueChangeEvent += OnConfigPropertyChanged;
			ConfigurationManager.Instance.ResetEvent += OnConfigReset;
			Toggle(Config.Enabled);
		}

		public static void Initialize()
		{
			Instance = new(ConfigurationManager.Instance.GetConfigObject<ElementHiderConfig>());
		}

		public static ElementHider Instance { get; private set; } = null!;

		protected override void InternalDispose()
		{
			_config.ValueChangeEvent -= OnConfigPropertyChanged;
			ConfigurationManager.Instance.ResetEvent -= OnConfigReset;
		}

		~ElementHider()
		{
			Dispose(false);
		}

		#endregion

		#region Configuration Events

		private void OnConfigPropertyChanged(object sender, OnChangeBaseArgs args)
		{
			switch (args.PropertyName)
			{
				case "Enabled":
#if DEBUG
					if (_debugConfig.LogConfigurationManager)
					{
						LogDebug("OnConfigPropertyChanged", $"{args.PropertyName}: {Config.Enabled}");
					}
#endif
					Toggle(Config.Enabled);
					break;

				case "HideActionBarLock":
#if DEBUG
					if (_debugConfig.LogConfigurationManager)
					{
						LogDebug("OnConfigPropertyChanged", $"{args.PropertyName}: {Config.HideActionBarLock}");
					}
#endif
					if (Config.Enabled)
					{
						_expectedVisibility[Element.ActionBarLock] = !Config.HideActionBarLock;
						UpdateAddons(new() {{Element.ActionBarLock, _expectedVisibility[Element.ActionBarLock]}});
					}

					break;
			}
		}

		private void OnConfigReset(ConfigurationManager sender)
		{
#if DEBUG
			if (_debugConfig.LogConfigurationManager)
			{
				LogDebug("OnConfigReset", "Resetting...");
			}
#endif
			Disable();
			if (_config != null)
			{
				_config.ValueChangeEvent -= OnConfigPropertyChanged;
			}

			_config = sender.GetConfigObject<ElementHiderConfig>();
			_config.ValueChangeEvent += OnConfigPropertyChanged;

#if DEBUG
			_debugConfig = sender.GetConfigObject<ElementHiderDebugConfig>();
			if (_debugConfig.LogConfigurationManager)
			{
				LogDebug("OnConfigReset", $"Config.Enabled: {Config.Enabled}");
			}
#endif
			Toggle(Config.Enabled);
		}

		#endregion
	}
}