/*
 * Based on:
 * - https://githubmate.com/repo/profK/RawInputLight
 * - https://referencesource.microsoft.com/#System.Windows.Forms/winforms/Managed/System/WinForms/Timer.cs,272
 * 
 * Notes:
 * - WM_SYSKEYDOWN will be sent as WM_KEYDOWN
 * - WM_SYSKEYUP will be sent as WM_KEYUP
 * - Keys sent after alt-tabbing out will be lost unless EnableBackgroundParsing is TRUE.
 * - ENABLE_BACKGROUND_PARSING is disabled by default (and should be), the state of pressed keys will be reset when the main window loses focus.
 * - Pressing ALT+GR is bad :D
 * 
 * Sample usage:
 * 
 * RawInputNativeWindow inputWin = new(Process.GetCurrentProcess().MainWindowHandle);
 * inputWin.CreateWindow();
 * inputWin.OnKeyStateChanged += OnKeyStateChanged;
 * 
 * Disposal:
 * 
 * inputWin.OnKeyStateChanged -= OnKeyStateChanged;
 * inputWin.Dispose(); // VERY IMPORTANT
 */

using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Accessibility;
using Windows.Win32.UI.Input;
using SezzUI.Logging;

namespace SezzUI.Game.RawInput
{
	public class RawInputNativeWindow : NativeWindow, IDisposable
	{
		#region Win32 API

		private const int WM_CLOSE = 0x0010;
		private const int WM_INPUT = 0x00FF;
		private const ushort HID_USAGE_PAGE_GENERIC = 0x01;
		private const ushort HID_USAGE_PAGE_GENERIC_DEVICE = 0x06;

		private const uint WINEVENT_OUTOFCONTEXT = 0;
		private const uint EVENT_SYSTEM_FOREGROUND = 3;

		#endregion

		internal PluginLogger Logger;

		/// <summary>
		///     Returns TRUE if the parent process is in foreground.
		/// </summary>
		private bool ShouldParse => ENABLE_BACKGROUND_PARSING || !IsInBackground;

		private readonly IntPtr _parentProcessHandle;
		private HWND _hwnd;

		/// <summary>
		///     Ignore repeated keys while holding down a key.
		/// </summary>
		public bool IgnoreRepeat = false;

		private static ushort? _lastVirtualKeyCode;
		private static KeyState? _lastKeyState;
		private static bool _devicesRegistered;

		/// <summary>
		///     Allow processing keys while parent process is not in foreground.
		/// </summary>
		private const bool ENABLE_BACKGROUND_PARSING = false;

		private bool IsInBackground => PInvoke.GetForegroundWindow() != _parentProcessHandle;

		private static WINEVENTPROC? _winEventHookProc;
		private UnhookWinEventSafeHandle? _winEventHookHandle;
		private CancellationTokenSource _unhookCts = new();
		private static readonly int _unhookTimeout = 5000;

		public delegate void OnKeyStateChangedDelegate(ushort vkCode, KeyState state);

		public event OnKeyStateChangedDelegate? OnKeyStateChanged;

		public RawInputNativeWindow(IntPtr parentProcessHandle)
		{
			Logger = new(GetType().Name);
			_parentProcessHandle = parentProcessHandle;
			_hwnd = new();
		}

		static RawInputNativeWindow()
		{
			_devicesRegistered = false;
		}

		protected override void OnHandleChange()
		{
#if DEBUG
			if (Plugin.DebugConfig.LogComponents && Plugin.DebugConfig.LogComponentsRawInputNativeWindow)
			{
				Logger.Debug($"Handle: {Handle.ToInt64():X}");
			}
#endif
			_hwnd = new(Handle);
		}

		public bool CreateWindow()
		{
			if (Handle == IntPtr.Zero)
			{
				try
				{
					// Microsoft uses HWND_MESSAGE as parent?
					// https://referencesource.microsoft.com/System.Windows.Forms/winforms/Managed/System/WinForms/NativeMethods.cs.html#bc3d3295a2b10729
					// private static HandleRef HWND_MESSAGE = new HandleRef(null, new IntPtr(-3));

					CreateHandle(new()
					{
						Style = 0,
						ExStyle = 0,
						ClassStyle = 0,
						Caption = GetType().Name,
						Parent = IntPtr.Zero
					});
				}
				catch (Exception ex)
				{
					Logger.Error(ex);
				}
			}

#if DEBUG
			if (Plugin.DebugConfig.LogComponents && Plugin.DebugConfig.LogComponentsRawInputNativeWindow)
			{
				Logger.Debug($"Handle: {Handle.ToInt64():X}");
			}
#endif

			if (!ENABLE_BACKGROUND_PARSING)
			{
				SetWinEventHook();
			}

			return Handle != IntPtr.Zero;
		}

		private bool InvokeRequired => GetInvokeRequired();

		private unsafe bool GetInvokeRequired()
		{
			if (Handle == IntPtr.Zero)
			{
				return false;
			}

			uint hwndThread = PInvoke.GetWindowThreadProcessId(_hwnd);
			uint currentThread = PInvoke.GetCurrentThreadId();
			bool invokeRequired = hwndThread != currentThread;

#if DEBUG
			if (invokeRequired && Plugin.DebugConfig.LogComponents && Plugin.DebugConfig.LogComponentsRawInputNativeWindow)
			{
				Logger.Debug($"Current Thread: {currentThread} Window Thread: {hwndThread}");
			}
#endif

			return invokeRequired;
		}

		#region WinEventHook

		private void SetWinEventHook()
		{
			if (_winEventHookHandle != null)
			{
				return;
			}

			try
			{
#if DEBUG
				if (Plugin.DebugConfig.LogComponents && Plugin.DebugConfig.LogComponentsRawInputNativeWindow)
				{
					Logger.Debug("Enabling hook");
				}
#endif
				// ReSharper disable once RedundantDelegateCreation
				_winEventHookProc = new(WinEventProc);
				_winEventHookHandle = PInvoke.SetWinEventHook(EVENT_SYSTEM_FOREGROUND, EVENT_SYSTEM_FOREGROUND, null, _winEventHookProc, 0, 0, WINEVENT_OUTOFCONTEXT);
			}
			catch (Exception ex)
			{
				Logger.Error(ex);
			}

			if (_winEventHookHandle == null || _winEventHookHandle.IsInvalid)
			{
				Logger.Error($"Error: {Marshal.GetLastWin32Error()}");
				_winEventHookHandle = null;
			}
#if DEBUG
			else if (Plugin.DebugConfig.LogComponents && Plugin.DebugConfig.LogComponentsRawInputNativeWindow)
			{
				Logger.Debug($"Handle: {_winEventHookHandle.DangerousGetHandle().ToInt64():X}");
			}
#endif
		}

		private void UnhookWinEvent()
		{
			if (_winEventHookHandle is {IsInvalid: false})
			{
				try
				{
					_winEventHookHandle.Dispose();
					_winEventHookHandle = null;
#if DEBUG
					if (Plugin.DebugConfig.LogComponents && Plugin.DebugConfig.LogComponentsRawInputNativeWindow)
					{
						Logger.Debug("Success!");
					}
#endif
				}
				catch (Exception ex)
				{
					Logger.Error(ex);
				}
			}
		}

		private void WinEventProc(HWINEVENTHOOK hWinEventHook, uint @event, HWND hwnd, int idObject, int idChild, uint idEventThread, uint dwmsEventTime)
		{
#if DEBUG
			if (Plugin.DebugConfig.LogComponents && Plugin.DebugConfig.LogComponentsRawInputNativeWindow)
			{
				Logger.Debug($"Event Type: {@event}");
			}
#endif

			if (_lastVirtualKeyCode != null && _lastKeyState is KeyState.KeyDown && IsInBackground)
			{
				TryInvokeOnKeyStateChanged((ushort) _lastVirtualKeyCode, KeyState.KeyUp);
			}
		}

		#endregion

		#region Finalizer

		private void DestroyWindow()
		{
			if (InvokeRequired)
			{
#if DEBUG
				if (Plugin.DebugConfig.LogComponents && Plugin.DebugConfig.LogComponentsRawInputNativeWindow)
				{
					Logger.Debug($"PostMessage: {Handle.ToInt64():X} WM_CLOSE");
				}
#endif
				_unhookCts = new(_unhookTimeout); // Gets cancelled after receiving WM_CLOSE and unhooking WinEvent!
				if (!PInvoke.PostMessage(_hwnd, WM_CLOSE, 0, 0))
				{
					Logger.Error($"PostMessage Error: {Marshal.GetLastWin32Error()}");
				}
				else
				{
					try
					{
						if (!_unhookCts.IsCancellationRequested)
						{
							_unhookCts.Token.WaitHandle.WaitOne(_unhookTimeout);
						}
					}
					catch (Exception ex)
					{
						Logger.Error(ex);
					}
					finally
					{
						if (!_unhookCts.IsCancellationRequested)
						{
							Logger.Error("Error: UnhookWinEvent timeout, something went terribly wrong.");
						}
					}
				}
			}
			else
			{
				UnhookWinEvent();
			}
		}

		public override void DestroyHandle()
		{
			_devicesRegistered = false;
			DestroyWindow();
			base.DestroyHandle();
		}

		public void Dispose()
		{
			DestroyHandle();
		}

		#endregion

		protected override void WndProc(ref Message m)
		{
			if (Handle == IntPtr.Zero)
			{
				return;
			}

			if (m.Msg == WM_INPUT)
			{
#if DEBUG
				if (Plugin.DebugConfig.LogComponents && Plugin.DebugConfig.LogComponentsRawInputNativeWindow)
				{
					Logger.Debug("WM_INPUT");
				}
#endif
				ParseRawInput(m.LParam);
				return;
			}

			if (m.Msg == WM_CLOSE)
			{
#if DEBUG
				if (Plugin.DebugConfig.LogComponents && Plugin.DebugConfig.LogComponentsRawInputNativeWindow)
				{
					Logger.Debug("WM_CLOSE");
				}
#endif
				UnhookWinEvent();
				_unhookCts.Cancel();
			}

			base.WndProc(ref m);
		}

		private void TryInvokeOnKeyStateChanged(ushort vkCode, KeyState keyState)
		{
			if (!IgnoreRepeat || _lastVirtualKeyCode == null || _lastKeyState == null || _lastVirtualKeyCode != vkCode || _lastKeyState != keyState)
			{
				_lastVirtualKeyCode = vkCode;
				_lastKeyState = keyState;

				try
				{
					OnKeyStateChanged?.Invoke(vkCode, keyState);
				}
				catch (Exception ex)
				{
					Logger.Error($"Failed invoking {nameof(OnKeyStateChanged)}: {ex}");
				}
			}
		}

		#region RawInput

		public unsafe bool RegisterDevices()
		{
			if (_devicesRegistered)
			{
				Logger.Error("Error: Only one window per raw input device class may be registered to receive raw input within a process!");
				return false;
			}

			if (Handle == IntPtr.Zero)
			{
				Logger.Error("Error: Window doesn't exist!");
				return false;
			}

			RAWINPUTDEVICE[] rawDevices =
			{
				new()
				{
					usUsagePage = HID_USAGE_PAGE_GENERIC,
					usUsage = HID_USAGE_PAGE_GENERIC_DEVICE,
					hwndTarget = new(Handle),
					dwFlags = RAWINPUTDEVICE_FLAGS.RIDEV_INPUTSINK
				}
			};

			fixed (RAWINPUTDEVICE* devPtr = rawDevices)
			{
#if DEBUG
				if (Plugin.DebugConfig.LogComponents && Plugin.DebugConfig.LogComponentsRawInputNativeWindow)
				{
					Logger.Debug($"Usage Page: {devPtr->usUsagePage} Usage ID: {devPtr->usUsage}");
				}
#endif
				try
				{
					if (PInvoke.RegisterRawInputDevices(devPtr, (uint) rawDevices.Length, (uint) sizeof(RAWINPUTDEVICE)))
					{
						_devicesRegistered = true;
						return true;
					}

					Logger.Error($"RegisterRawInputDevices Error: {Marshal.GetLastWin32Error()}");
					return false;
				}
				catch (Exception ex)
				{
					Logger.Error(ex);
					return false;
				}
			}
		}

		private unsafe void ParseRawInput(IntPtr lParam)
		{
			if (lParam == IntPtr.Zero || !ShouldParse)
			{
				return;
			}

			uint dwSize;
			if (PInvoke.GetRawInputData(new(lParam), RAW_INPUT_DATA_COMMAND_FLAGS.RID_INPUT, IntPtr.Zero.ToPointer(), &dwSize, (uint) sizeof(RAWINPUTHEADER)) == 0 && dwSize != 0)
			{
				IntPtr lpb = Marshal.AllocHGlobal((int) dwSize);

				if (PInvoke.GetRawInputData(new(lParam), RAW_INPUT_DATA_COMMAND_FLAGS.RID_INPUT, lpb.ToPointer(), &dwSize, (uint) sizeof(RAWINPUTHEADER)) != dwSize)
				{
					Logger.Error("Error: GetRawInputData did not return the correct size!");
				}
				else
				{
					RAWINPUT* raw = (RAWINPUT*) lpb;
					if (raw->header.dwType == 1u) // Keyboard
					{
						switch ((KeyState) raw->data.keyboard.Message)
						{
							case KeyState.SysKeyDown:
							case KeyState.KeyDown:
								TryInvokeOnKeyStateChanged(raw->data.keyboard.VKey, KeyState.KeyDown);
								break;

							case KeyState.SysKeyUp:
							case KeyState.KeyUp:
								TryInvokeOnKeyStateChanged(raw->data.keyboard.VKey, KeyState.KeyUp);
								break;
						}
					}
				}

				Marshal.FreeHGlobal(lpb);
			}
			else
			{
				Logger.Error("GetRawInputData Error: Unsuccessful.");
			}
		}

		#endregion
	}
}