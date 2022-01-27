using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;
using SezzUI.Config;
using SezzUI.Enums;
using SezzUI.GameEvents;
using SezzUI.GameStructs;
using SezzUI.Interface.GeneralElements;
using SezzUI.NativeMethods;
using SezzUI.NativeMethods.RawInput;

namespace SezzUI.Modules.GameUI
{
	public class ActionBar : HudModule
	{
		private static readonly Dictionary<ActionBarLayout, Vector2<byte>> _dimensions = new()
		{
			{ActionBarLayout.H12V1, new(12, 1)},
			{ActionBarLayout.H6V2, new(6, 2)},
			{ActionBarLayout.H4V3, new(4, 3)},
			{ActionBarLayout.H3V4, new(3, 4)},
			{ActionBarLayout.H2V6, new(2, 6)},
			{ActionBarLayout.H1V12, new(1, 12)}
		};

		private static readonly byte _maxButtons = 12;
		private readonly Dictionary<Element, Dictionary<uint, Vector2<float>>> _originalPositions = new();
#if DEBUG
		private readonly ActionBarDebugConfig _debugConfig;
#endif

		private SetActionBarPageDelegate? _setActionBarPage;
		private IntPtr? _setPagePtr;
		private ActionBarConfig Config => (ActionBarConfig) _config;

		protected override bool Enable()
		{
			if (!base.Enable())
			{
				return false;
			}

			EventManager.Game.AddonsLoaded += OnAddonsLoaded;
			EventManager.Game.HudLayoutActivated += OnHudLayoutActivated;

			ScanBarPagingSignatures();

			if (EventManager.Game.IsInGame())
			{
				Update();
			}

			EnableBarPaging();

			return true;
		}

		protected override bool Disable()
		{
			if (!base.Disable())
			{
				return false;
			}

			EventManager.Game.AddonsLoaded -= OnAddonsLoaded;
			EventManager.Game.HudLayoutActivated -= OnHudLayoutActivated;

			Reset();
			_originalPositions.Clear();
			DisableBarPaging();

			return true;
		}

		private void Update()
		{
			// TODO
			UpdateActionBar(Element.ActionBar1, Config.Bar1);
			UpdateActionBar(Element.ActionBar2, Config.Bar2);
			UpdateActionBar(Element.ActionBar3, Config.Bar3);
			UpdateActionBar(Element.ActionBar4, Config.Bar4);
			UpdateActionBar(Element.ActionBar5, Config.Bar5);
			UpdateActionBar(Element.ActionBar6, Config.Bar6);
			UpdateActionBar(Element.ActionBar7, Config.Bar7);
			UpdateActionBar(Element.ActionBar8, Config.Bar8);
			UpdateActionBar(Element.ActionBar9, Config.Bar9);
			UpdateActionBar(Element.ActionBar10, Config.Bar10);
		}

		private unsafe void UpdateActionBar(Element bar, SingleActionBarConfig config)
		{
			if (!config.Enabled)
			{
				return;
			}

#if DEBUG
			if (_debugConfig.LogLayout)
			{
				Logger.Debug("UpdateActionBar", $"[{bar}] Updating...");
			}
#endif
			AtkUnitBase* addon = (AtkUnitBase*) Plugin.GameGui.GetAddonByName(Addons.Names[bar], 1);
			if ((IntPtr) addon != IntPtr.Zero)
			{
				ActionBarLayout layout = ((AddonActionBarBase*) addon)->Layout;
				switch (layout)
				{
					case ActionBarLayout.H12V1:
					case ActionBarLayout.H6V2:
					case ActionBarLayout.H4V3:
					case ActionBarLayout.H3V4:
					case ActionBarLayout.H2V6:
					case ActionBarLayout.H1V12:
#if DEBUG
						if (_debugConfig.LogLayout)
						{
							Logger.Debug("UpdateActionBar", $"[{bar}] Layout: {layout}");
						}
#endif
						bool updateNodes = addon->UldManager.NodeListCount == 0;
						if (updateNodes)
						{
							addon->UldManager.UpdateDrawNodeList();
						}

						if (CacheActionBarPositions(bar, (IntPtr) addon))
						{
							if (config.InvertRowOrdering)
							{
								InvertActionBarRows(bar, (IntPtr) addon, layout);
							}
							else
							{
								// TODO: Only reset row ordering here instead of everything!
								ResetActionBar(bar, config);
							}
						}

						if (updateNodes)
						{
							addon->UldManager.NodeListCount = 0;
						}

						break;

					case ActionBarLayout.Unknown:
						Logger.Error("UpdateActionBar", $"[{bar}] Error: Unsupported Layout ID: {((AddonActionBarBase*) addon)->LayoutID}");
						break;

					default:
						Logger.Error("UpdateActionBar", $"[{bar}] Error: Unsupported Layout: {layout}");
						break;
				}
			}
			else
			{
				Logger.Error("UpdateActionBar", $"[{bar}] Error: Invalid addon: {Addons.Names[bar]}");
			}
		}

		private unsafe void InvertActionBarRows(Element bar, IntPtr addonPtr, ActionBarLayout layout)
		{
#if DEBUG
			if (_debugConfig.LogLayout)
			{
				Logger.Debug("InvertActionBarRows", $"[{bar}] Inverting rows...");
			}
#endif
			AtkUnitBase* addon = (AtkUnitBase*) addonPtr;

			if (_dimensions[layout].Y > 1)
			{
				List<uint> nodeIds = _originalPositions[bar].Keys.ToList();
				nodeIds.Sort();

				for (byte sourceRow = 0; sourceRow < _dimensions[layout].Y / 2; sourceRow++)
				{
					byte targetRow = (byte) (_dimensions[layout].Y - 1 - sourceRow);
#if DEBUG
					if (_debugConfig.LogLayout)
					{
						Logger.Debug("InvertActionBarRows", $"[{bar}] Swapping rows: {sourceRow} <> {targetRow}");
					}
#endif
					for (byte sourceButtonBase = 0; sourceButtonBase < _dimensions[layout].X; sourceButtonBase++)
					{
						byte sourceButton = (byte) (sourceButtonBase + _dimensions[layout].X * sourceRow);
						byte targetButton = (byte) (_dimensions[layout].X * targetRow + sourceButtonBase);
#if DEBUG
						if (_debugConfig.LogLayout)
						{
							Logger.Debug("InvertActionBarRows", $"[{bar}] Swapping buttons: {sourceButton} ({nodeIds[sourceButton]}) <> {targetButton} ({nodeIds[targetButton]})");
						}
#endif
						AtkResNode* sourceNode = addon->GetNodeById(nodeIds[sourceButton]);
						AtkResNode* targetNode = addon->GetNodeById(nodeIds[targetButton]);

						if (sourceNode != null && targetNode != null)
						{
							sourceNode->SetPositionFloat(_originalPositions[bar][nodeIds[targetButton]].X, _originalPositions[bar][nodeIds[targetButton]].Y);
							targetNode->SetPositionFloat(_originalPositions[bar][nodeIds[sourceButton]].X, _originalPositions[bar][nodeIds[sourceButton]].Y);
						}
						else
						{
							Logger.Error("InvertActionBarRows", $"[{bar}] Error: Nodes not found!");
						}
					}
				}
			}
		}

		private unsafe bool CacheActionBarPositions(Element bar, IntPtr addonPtr)
		{
			AtkUnitBase* addon = (AtkUnitBase*) addonPtr;

			if (_originalPositions.ContainsKey(bar))
			{
#if DEBUG
				if (_debugConfig.LogLayout)
				{
					Logger.Debug("CacheActionBarPositions", $"[{bar}] Ignored, already cached!");
				}
#endif
				return true;
			}

			if (addon->UldManager.NodeListCount == 0)
			{
				Logger.Error("CacheActionBarPositions", $"[{bar}] Error: Addon has no child nodes!");
				return false;
			}

			byte buttonsFound = 0;

			lock (_originalPositions)
			{
				_originalPositions[bar] = new();

				for (int j = 0; j < addon->UldManager.NodeListCount; j++)
				{
					AtkResNode* node = addon->UldManager.NodeList[j];
					if (node != null && (int) node->Type >= 1000)
					{
						AtkComponentNode* compNode = (AtkComponentNode*) node;
						AtkUldComponentInfo* objectInfo = (AtkUldComponentInfo*) compNode->Component->UldManager.Objects;

						if (objectInfo->ComponentType == ComponentType.Base)
						{
							// This should be an ActionButton!
#if DEBUG
							if (_debugConfig.LogLayout)
							{
								Logger.Debug("CacheActionBarPositions", $"[{bar}] Caching node: ID: {node->NodeID} X: {node->X} Y: {node->Y}");
							}
#endif
							_originalPositions[bar].Add(node->NodeID, new(node->X, node->Y));
							buttonsFound++;
						}
					}
				}
			}

			if (buttonsFound != _maxButtons)
			{
				Logger.Error("CacheActionBarPositions", $"[{bar}] Error: Invalid amount of buttons: {buttonsFound}/{_maxButtons}");
				_originalPositions.Remove(bar);
				return false;
			}

			return true;
		}

		private void Reset()
		{
			// TODO
			ResetActionBar(Element.ActionBar1, Config.Bar1);
			ResetActionBar(Element.ActionBar2, Config.Bar2);
			ResetActionBar(Element.ActionBar3, Config.Bar3);
			ResetActionBar(Element.ActionBar4, Config.Bar4);
			ResetActionBar(Element.ActionBar5, Config.Bar5);
			ResetActionBar(Element.ActionBar6, Config.Bar6);
			ResetActionBar(Element.ActionBar7, Config.Bar7);
			ResetActionBar(Element.ActionBar8, Config.Bar8);
			ResetActionBar(Element.ActionBar9, Config.Bar9);
			ResetActionBar(Element.ActionBar10, Config.Bar10);
		}

		private unsafe void ResetActionBar(Element bar, SingleActionBarConfig config)
		{
			if (!_originalPositions.ContainsKey(bar))
			{
				return;
			}

#if DEBUG
			if (_debugConfig.LogLayout)
			{
				Logger.Debug("ResetActionBar", $"[{bar}] Resetting element: {Addons.Names[bar]}");
			}
#endif

			AtkUnitBase* addon = (AtkUnitBase*) Plugin.GameGui.GetAddonByName(Addons.Names[bar], 1);
			if ((IntPtr) addon != IntPtr.Zero)
			{
				foreach ((uint nodeId, Vector2<float> pos) in _originalPositions[bar])
				{
					AtkResNode* node = addon->GetNodeById(nodeId);
					if (node != null)
					{
						node->SetPositionFloat(pos.X, pos.Y);
					}
					else
					{
						Logger.Error("ResetActionBar", $"[{bar}] Error: Invalid node ID: {nodeId}");
					}
				}
			}
			else
			{
				Logger.Error("ResetActionBar", $"[{bar}] Error: Invalid addon: {Addons.Names[bar]}");
			}
		}

		private void OnHudLayoutActivated(uint hudLayout, bool ready)
		{
			// Discard cache and update, switching HUD layout resets positions.
			// TODO: This also happens right after logging in (OnAddonsLoaded)?
			_originalPositions.Clear();

			if (ready)
			{
				Update();
			}
		}

		private void OnAddonsLoaded(bool loaded, bool ready)
		{
			if (loaded && ready)
			{
				// Force update!
				Update();
			}
			else
			{
				// Discard cache. ActionBars will reset when the game re-enables addons, we don't have reset them here.
				_originalPositions.Clear();
			}
		}

		private delegate void SetActionBarPageDelegate(IntPtr agentActionBar, byte page);

		#region Bar Paging

		private readonly byte _pageDefault = 0;
		private readonly ushort VK_CONTROL = 0x11;
		private readonly ushort VK_MENU = 0x12;
		private static RawInputNativeWindow? _msgWindow;

		private void ScanBarPagingSignatures()
		{
			if (_setPagePtr != null)
			{
				return;
			}

			if (Plugin.SigScanner.TryScanText("E8 ?? ?? ?? ?? FF C3 83 FB 38 7E E0 BA ?? ?? ?? ?? 48 8B CF", out IntPtr setPagePtr)) // 6.0.5: ffxiv_dx11.exe+80D130
			{
#if DEBUG
				if (_debugConfig.LogSigScanner)
				{
					Logger.Debug("ScanBarPagingSignatures", $"SetPage Address: {setPagePtr.ToInt64():X}");
				}
#endif
				_setPagePtr = setPagePtr;
				try
				{
					_setActionBarPage = Marshal.GetDelegateForFunctionPointer<SetActionBarPageDelegate>(setPagePtr);
				}
				catch (Exception ex)
				{
					Logger.Error(ex, "ScanBarPagingSignatures", $"Error: {ex}");
				}
			}
			else
			{
				Logger.Error("ScanBarPagingSignatures", "Error: SetPage signature not found!");
			}
		}

		public override void Draw(DrawState state, Vector2? origin)
		{
			if (_msgWindow != null && _msgWindow.Handle == IntPtr.Zero)
			{
				bool success = false;
				if (!_msgWindow.CreateWindow())
				{
					Logger.Error("Draw", "Error: Failed to setup Bar Paging, CreateWindow failed!");
				}
				else if (!_msgWindow.RegisterDevices())
				{
					Logger.Error("Draw", "Error: Failed to setup Bar Paging: RegisterDevices failed! Bar Paging will now be disabled!");
				}
				else
				{
#if DEBUG
					if (_debugConfig.LogBarPaging)
					{
						Logger.Debug("Draw", "Bar Paging enabled, RawInputNativeWindow successfully created.");
					}
#endif
					success = true;
					_msgWindow.OnKeyStateChanged += OnKeyStateChanged;
				}

				if (!success)
				{
					Plugin.ChatGui.PrintError("SezzUI failed to setup Bar Paging - it will now be disabled and can only be enabled again after RESTARTING the game!");
					Plugin.ChatGui.PrintError("You can (and should) check the Dalamud logfile for further details.");
					Config.EnableBarPaging = false;
					_msgWindow.Dispose();
					_msgWindow = null;
				}
			}
		}

		private void OnKeyStateChanged(ushort vkCode, KeyState state)
		{
#if DEBUG
			// if (_debugConfig.LogRawInputEventHandling)
			// {
			// 	Logger.Debug("OnKeyStateChanged", $"Key: {vkCode} State: {state}");
			// }
#endif

			if (state == KeyState.KeyDown)
			{
				if (vkCode == VK_CONTROL)
				{
#if DEBUG
					if (_debugConfig.LogRawInputEventHandling)
					{
						Logger.Debug("OnKeyStateChanged", $"Key: {vkCode} State: {state} (HoldingCtrl)");
					}
#endif
					SetActionBarPage((byte) Config.BarPagingPageCtrl);
				}
				else if (vkCode == VK_MENU)
				{
#if DEBUG
					if (_debugConfig.LogRawInputEventHandling)
					{
						Logger.Debug("OnKeyStateChanged", $"Key: {vkCode} State: {state} (HoldingAlt)");
					}
#endif
					SetActionBarPage((byte) Config.BarPagingPageAlt);
				}
			}
			else if (state == KeyState.KeyUp)
			{
				if (vkCode != VK_CONTROL && vkCode != VK_MENU)
				{
					// Ignore
					return;
				}

				// TODO: GetAsyncKeyState
				bool pressedCtrl = ImGui.GetIO().KeyCtrl;
				bool pressedAlt = ImGui.GetIO().KeyAlt;

				if (!pressedAlt && !pressedCtrl)
				{
					// No modifiers, we should never get here :D
#if DEBUG
					if (_debugConfig.LogRawInputEventHandling)
					{
						Logger.Debug("OnKeyStateChanged", $"Key: {vkCode} State: {state} Ctrl: {pressedCtrl} Alt {pressedAlt} (NoModifiers)");
					}
#endif
					Logger.Debug("OnKeyUp", $"{vkCode} NoModifiers");
					SetActionBarPage(0);
				}
				else if (vkCode == VK_MENU)
				{
					// Released Alt
#if DEBUG
					if (_debugConfig.LogRawInputEventHandling)
					{
						Logger.Debug("OnKeyStateChanged", $"Key: {vkCode} State: {state} Ctrl: {pressedCtrl} Alt {pressedAlt} (ReleasedAlt)");
					}
#endif
					SetActionBarPage(pressedCtrl ? (byte) Config.BarPagingPageCtrl : _pageDefault);
				}
				else if (vkCode == VK_CONTROL)
				{
					// Released Ctrl
#if DEBUG
					if (_debugConfig.LogRawInputEventHandling)
					{
						Logger.Debug("OnKeyStateChanged", $"Key: {vkCode} State: {state} Ctrl: {pressedCtrl} Alt {pressedAlt} (ReleasedCtrl)");
					}
#endif
					SetActionBarPage(pressedAlt ? (byte) Config.BarPagingPageAlt : _pageDefault);
				}
			}
		}

		private void EnableBarPaging()
		{
			if (!Config.EnableBarPaging || _msgWindow != null)
			{
				return;
			}

			if (_setPagePtr == null)
			{
				Plugin.ChatGui.PrintError("Signature scan failed, Bar Paging will not be available!");
			}
			else
			{
				_msgWindow = new(Process.GetCurrentProcess().MainWindowHandle) {IgnoreRepeat = true};
			}
		}

		private void DisableBarPaging()
		{
			if (_msgWindow == null)
			{
				return;
			}

			_msgWindow.OnKeyStateChanged -= OnKeyStateChanged;
			_msgWindow.Dispose();
			_msgWindow = null;
		}

		private void ToggleBarPaging(bool enable)
		{
			if (enable)
			{
				EnableBarPaging();
			}
			else
			{
				DisableBarPaging();
			}
		}

		private unsafe void SetActionBarPage(byte page)
		{
			if (!EventManager.Game.AreAddonsReady || _setActionBarPage == null)
			{
#if DEBUG
				if (_debugConfig.LogBarPaging)
				{
					Logger.Debug("SetActionBarPage", $"EventManager.Game.AreAddonsReady: {EventManager.Game.AreAddonsReady} _setActionBarPage {_setActionBarPage}");
				}
#endif
				return;
			}

			AtkUnitBase* actionBar = (AtkUnitBase*) Plugin.GameGui.GetAddonByName("_ActionBar", 1);
			if ((IntPtr) actionBar == IntPtr.Zero || !actionBar->IsVisible)
			{
#if DEBUG
				if (_debugConfig.LogBarPaging)
				{
					Logger.Debug("SetActionBarPage", $"_ActionBar: {((IntPtr) actionBar).ToInt64():X} IsVisible {actionBar->IsVisible}");
				}
#endif
				return;
			}

			AddonActionBarBase* actionBarBase = (AddonActionBarBase*) actionBar;

			if (actionBarBase->HotbarID < 10 && actionBarBase->HotbarID != page)
			{
#if DEBUG
				if (_debugConfig.LogBarPaging)
				{
					Logger.Debug("SetActionBarPage", $"Page: {page}");
				}
#endif

				IntPtr agentActionBar = Plugin.GameGui.FindAgentInterface(actionBar);
				if (agentActionBar != IntPtr.Zero)
				{
					try
					{
						_setActionBarPage(agentActionBar, page);
					}
					catch (Exception ex)
					{
						Logger.Error(ex, "SetActionBarPage", $"Error: {ex}");
					}
				}
			}
#if DEBUG
			else if (_debugConfig.LogBarPaging)
			{
				Logger.Debug("SetActionBarPage", $"IsPetHotbar: {actionBarBase->HasPetHotbar} HotbarID {actionBarBase->HotbarID}");
			}
#endif
		}

		#endregion

		#region Singleton

		private ActionBar(ActionBarConfig config) : base(config)
		{
#if DEBUG
			_debugConfig = ConfigurationManager.Instance.GetConfigObject<ActionBarDebugConfig>();
#endif
			Config.ValueChangeEvent += OnConfigPropertyChanged;
			Config.Bar1.ValueChangeEvent += OnConfigPropertyChanged;
			Config.Bar2.ValueChangeEvent += OnConfigPropertyChanged;
			Config.Bar3.ValueChangeEvent += OnConfigPropertyChanged;
			Config.Bar4.ValueChangeEvent += OnConfigPropertyChanged;
			Config.Bar5.ValueChangeEvent += OnConfigPropertyChanged;
			Config.Bar6.ValueChangeEvent += OnConfigPropertyChanged;
			Config.Bar7.ValueChangeEvent += OnConfigPropertyChanged;
			Config.Bar8.ValueChangeEvent += OnConfigPropertyChanged;
			Config.Bar9.ValueChangeEvent += OnConfigPropertyChanged;
			Config.Bar10.ValueChangeEvent += OnConfigPropertyChanged;

			ConfigurationManager.Instance.Reset += OnConfigReset;
			Enable();
		}

		public static void Initialize()
		{
			Instance = new(ConfigurationManager.Instance.GetConfigObject<ActionBarConfig>());
		}

		public static ActionBar Instance { get; private set; } = null!;

		protected override void InternalDispose()
		{
			Disable();
			Config.Bar1.ValueChangeEvent -= OnConfigPropertyChanged;
			Config.Bar2.ValueChangeEvent -= OnConfigPropertyChanged;
			Config.Bar3.ValueChangeEvent -= OnConfigPropertyChanged;
			Config.Bar4.ValueChangeEvent -= OnConfigPropertyChanged;
			Config.Bar5.ValueChangeEvent -= OnConfigPropertyChanged;
			Config.Bar6.ValueChangeEvent -= OnConfigPropertyChanged;
			Config.Bar7.ValueChangeEvent -= OnConfigPropertyChanged;
			Config.Bar8.ValueChangeEvent -= OnConfigPropertyChanged;
			Config.Bar9.ValueChangeEvent -= OnConfigPropertyChanged;
			Config.Bar10.ValueChangeEvent -= OnConfigPropertyChanged;
			Config.ValueChangeEvent -= OnConfigPropertyChanged;
			ConfigurationManager.Instance.Reset -= OnConfigReset;
		}

		~ActionBar()
		{
			Dispose(false);
		}

		#endregion

		#region Configuration Events

		private void OnConfigPropertyChanged(object sender, OnChangeBaseArgs args)
		{
			if (sender is SingleActionBarConfig barConfig)
			{
				switch (args.PropertyName)
				{
					case "Enabled":
#if DEBUG
						if (_debugConfig.LogConfigurationManager)
						{
							Logger.Debug("OnConfigPropertyChanged", $"{sender.GetType().Name}: {args.PropertyName}: {barConfig.Enabled}");
						}
#endif
						if (barConfig.Enabled)
						{
							if (Game.Instance.IsInGame())
							{
								// Update bar now.
								// If not ingame all bars will be updated later anyways.
								UpdateActionBar(barConfig.Bar, barConfig);
							}
						}
						else
						{
							if (Game.Instance.IsInGame())
							{
								ResetActionBar(barConfig.Bar, barConfig);
							}

							_originalPositions.Remove(barConfig.Bar);
						}

						break;

					case "InvertRowOrdering":
#if DEBUG
						if (_debugConfig.LogConfigurationManager)
						{
							Logger.Debug("OnConfigPropertyChanged", $"{sender.GetType().Name}: {args.PropertyName}: {barConfig.InvertRowOrdering}");
						}
#endif
						if (barConfig.Enabled && Game.Instance.IsInGame())
						{
							UpdateActionBar(barConfig.Bar, barConfig);
						}

						break;
				}
			}
			else
			{
				switch (args.PropertyName)
				{
					case "EnableBarPaging":
#if DEBUG
						if (_debugConfig.LogConfigurationManager)
						{
							Logger.Debug("OnConfigPropertyChanged", $"{args.PropertyName}: {Config.EnableBarPaging}");
						}
#endif
						ToggleBarPaging(Config.EnableBarPaging);
						break;
				}
			}
		}

		private void OnConfigReset(ConfigurationManager sender, PluginConfigObject config)
		{
			if (config is not ActionBarConfig)
			{
				return;
			}
#if DEBUG
			if (_debugConfig.LogConfigurationManager)
			{
				Logger.Debug("OnConfigReset", "Resetting...");
			}
#endif
			Reset();
			Update();
		}

		#endregion
	}

	internal class Vector2<T>
	{
		public Vector2(T x, T y)
		{
			X = x;
			Y = y;
		}

		public T X { get; set; }
		public T Y { get; set; }
	}
}