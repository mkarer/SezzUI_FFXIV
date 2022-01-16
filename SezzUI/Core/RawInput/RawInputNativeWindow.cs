/*
 * Based on:
 * - https://githubmate.com/repo/profK/RawInputLight
 * - https://referencesource.microsoft.com/#System.Windows.Forms/winforms/Managed/System/WinForms/Timer.cs,272
 * 
 * Notes:
 * - WM_SYSKEYDOWN will be sent as WM_KEYDOWN
 * - WM_SYSKEYUP will be sent as WM_KEYUP
 * - Keys sent while/after alt-tabbing out will be lost unless AllowBackground is TRUE.
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
 * inputWin.DestroyHandle();
 * 
 */
// https://githubmate.com/repo/profK/RawInputLight
// https://referencesource.microsoft.com/#System.Windows.Forms/winforms/Managed/System/WinForms/Timer.cs,272

using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Runtime.Versioning;
using Windows.Win32;
using Dalamud.Logging;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Input;

namespace SezzUI.NativeMethods.RawInput
{
	public class RawInputNativeWindow : NativeWindow
	{
		#region Win32 API
		[DllImport("user32.dll", CharSet = CharSet.Auto)]
		[ResourceExposure(ResourceScope.None)]
		private static extern IntPtr PostMessage(HandleRef hwnd, int msg, int wparam, int lparam);

		[DllImport("user32.dll", ExactSpelling = true, CharSet = CharSet.Auto)]
		[ResourceExposure(ResourceScope.Process)]
		private static extern int GetWindowThreadProcessId(HandleRef hWnd, out int lpdwProcessId);

		[DllImport("kernel32.dll", ExactSpelling = true, CharSet = CharSet.Auto)]
		[ResourceExposure(ResourceScope.Process)]
		private static extern int GetCurrentThreadId();

		private const int WM_CLOSE = 0x0010;
		private const int WM_INPUT = 0x00FF;
		private const ushort HID_USAGE_PAGE_GENERIC = 0x01;
		private const ushort HID_USAGE_PAGE_GENERIC_DEVICE = 0x06;
		#endregion

		/// <summary>
		/// Returns TRUE if the parent process is in foreground.
		/// </summary>
		public bool ShouldParse => AllowBackgroundParsing || !IsInBackground;
		private readonly IntPtr _parentProcessHandle;

		private HandleRef _handleRef = new();

		/// <summary>
		/// Ignore repeated keys while holding down a key.
		/// </summary>
		public bool IgnoreRepeat = false;
		private ushort? _lastVirtualKeyCode;
		private KeyState? _lastKeyState;

		/// <summary>
		/// Allow processing keys while parent process is not in foreground.
		/// </summary>
		public bool AllowBackgroundParsing = false;
		public bool IsInBackground => PInvoke.GetForegroundWindow() != _parentProcessHandle;
	
		public delegate void OnKeyStateChangedDelegate(ushort vkCode, KeyState state);
		public event OnKeyStateChangedDelegate? OnKeyStateChanged;

		public RawInputNativeWindow(IntPtr parentProcessHandle)
		{
			_parentProcessHandle = parentProcessHandle;
		}

		protected override void OnHandleChange()
		{
			PluginLog.Debug($"OnHandleChange: {Handle.ToInt64():X}");
			_handleRef = new(this, Handle);
		}

		public bool CreateWindow()
		{
			if (Handle == IntPtr.Zero)
			{
				PluginLog.Debug("CreateWindow");
				try
				{
					// Microsoft uses HWND_MESSAGE as parent?
					// https://referencesource.microsoft.com/System.Windows.Forms/winforms/Managed/System/WinForms/NativeMethods.cs.html#bc3d3295a2b10729
					// private static HandleRef HWND_MESSAGE = new HandleRef(null, new IntPtr(-3));

					CreateHandle(new CreateParams
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
					PluginLog.Error(ex, $"CreateWindow Error: {ex}");
				}
			}

			PluginLog.Debug($"CreateWindow Handle: {Handle.ToInt64():X}");

			return Handle != IntPtr.Zero;
		}

		public bool InvokeRequired => GetInvokeRequired(Handle);

		private bool GetInvokeRequired(IntPtr hWnd)
		{
            if (hWnd == IntPtr.Zero) { return false; }

			var hwndThread = GetWindowThreadProcessId(_handleRef, out int pid);
			var currentThread = GetCurrentThreadId();
			bool invokeRequired = hwndThread != currentThread;

			if (invokeRequired)
			{
				PluginLog.Debug($"GetInvokeRequired hwndThread: {hwndThread} currentThread {currentThread}");
			}

			return invokeRequired;
		}

		#region Finalizer
		public void DestroyWindow()
		{
			DestroyWindow(true);
		}

		private void DestroyWindow(bool destroyHandle)
		{
			if (GetInvokeRequired(Handle))
			{
				PostMessage(_handleRef, WM_CLOSE, 0, 0);
				return;
			}

			lock (this)
			{
				if (destroyHandle)
				{
					base.DestroyHandle();
				}
			}
		}

		public override void DestroyHandle()
		{
            _devicesRegistered = false;
            DestroyWindow(false);
			base.DestroyHandle();
		}
		#endregion

		protected override void WndProc(ref Message m)
		{
			if (Handle == IntPtr.Zero)
			{
				return;
			}
			else if (m.Msg == WM_INPUT)
			{
				//PluginLog.Debug("WM_INPUT");
				ParseRawInput(m.LParam);
				return;
			}
			else
			{
				base.WndProc(ref m);
			}
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
					PluginLog.Error(ex, $"Failed invoking {nameof(OnKeyStateChanged)}: {ex}");
				}
			}
		}

		#region RawInput
		private static bool _devicesRegistered = false;

		public unsafe bool RegisterDevices()
		{
			PluginLog.Debug("RegisterDevices");

			if (_devicesRegistered)
			{
				PluginLog.Error("RegisterDevices Error: Only one window per raw input device class may be registered to receive raw input within a process!");
				return false;
			}
			else if (Handle == IntPtr.Zero)
			{
				PluginLog.Error("RegisterDevices Error: Window doesn't exist!");
				return false;
			}

			RAWINPUTDEVICE[] rawDevices = new RAWINPUTDEVICE[1]
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
				PluginLog.Debug($"RegisterRawInputDevices: {devPtr->usUsagePage} {devPtr->usUsage}");
				try
				{
					if (PInvoke.RegisterRawInputDevices(devPtr, (uint)rawDevices.Length, (uint)sizeof(RAWINPUTDEVICE)))
					{
						_devicesRegistered = true;
						return true;
					}
					else
					{
						PluginLog.Error("RegisterRawInputDevices Error: " + Marshal.GetLastWin32Error());
						return false;
					}
				}
				catch (Exception ex)
				{
					PluginLog.Error(ex, $"RegisterRawInputDevices Error: {ex}");
					return false;
				}
			}
		}

		private unsafe void ParseRawInput(IntPtr lParam)
		{
			if (lParam == IntPtr.Zero || !ShouldParse) { return; }

			uint dwSize;
			if (PInvoke.GetRawInputData(new HRAWINPUT(lParam), RAW_INPUT_DATA_COMMAND_FLAGS.RID_INPUT, IntPtr.Zero.ToPointer(), &dwSize, (uint)sizeof(RAWINPUTHEADER)) == 0 && dwSize != 0)
			{
				var lpb = Marshal.AllocHGlobal((int)dwSize);

				if (PInvoke.GetRawInputData(new HRAWINPUT(lParam), RAW_INPUT_DATA_COMMAND_FLAGS.RID_INPUT, lpb.ToPointer(), &dwSize, (uint)sizeof(RAWINPUTHEADER)) != dwSize)
				{
					PluginLog.Error($"GetRawInputData did not return the correct size!");
				}
				else
				{
					var raw = (RAWINPUT*)lpb;
					HANDLE devHandle = raw->header.hDevice;

					if (raw->header.dwType == 1u) // Keyboard
					{
						switch ((KeyState)raw->data.keyboard.Message)
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
				PluginLog.Error($"GetRawInputData failed.");
			}
		}
		#endregion
	}
}
