using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SezzUI.Config;
using SezzUI.Interface.GeneralElements;
using System.ComponentModel;
using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Dalamud.Game.Gui;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using System.Runtime.InteropServices;
using System.Numerics;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using Gma.System.MouseKeyHook;

namespace SezzUI
{
    internal class NativeMethods : IDisposable
    {
        #region API
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, MessageProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool UnhookWindowsHookEx(IntPtr hHook);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern IntPtr GetForegroundWindow();

        public delegate IntPtr MessageProc(int nCode, IntPtr wParam, IntPtr lParam);

        public IKeyboardMouseEvents? m_GlobalHook = null;

        //[StructLayout(LayoutKind.Sequential)]
        //public class KBDLLHOOKSTRUCT
        //{
        //    public uint vkCode;
        //    public uint scanCode;
        //    public KBDLLHOOKSTRUCTFlags flags;
        //    public uint time;
        //    public UIntPtr dwExtraInfo;
        //}

        //[Flags]
        //public enum KBDLLHOOKSTRUCTFlags : uint
        //{
        //    LLKHF_EXTENDED = 0x01,
        //    LLKHF_INJECTED = 0x10,
        //    LLKHF_ALTDOWN = 0x20,
        //    LLKHF_UP = 0x80,
        //}

        private const int WH_KEYBOARD = 2;
        private const int WH_KEYBOARD_LL = 13;
        public const int VK_LCONTROL = 0xA2; // 162
        public const int VK_RCONTROL = 0xA3; // 163
        public const int VK_LMENU = 0xA4; // 164
        public const int VK_RMENU = 0xA5; // 164
        #endregion

        public enum KeyboardMessage
        {
            KeyDown = 0x100,
            KeyUp = 0x101,
            SysKeyDown = 0x104,
            SysKeyUp = 0x105
        }

        //public event EventHandler<NewKeyboardMessageEventArgs>? NewKeyboardMessage;
        //public class NewKeyboardMessageEventArgs : EventArgs
        //{
        //    public uint VirtKeyCode { get; private set; }
        //    public KeyboardMessage MessageType { get; private set; }

        //    public NewKeyboardMessageEventArgs(uint vkCode, KeyboardMessage msg)
        //    {
        //        VirtKeyCode = vkCode;
        //        MessageType = msg;
        //    }
        //}

        public delegate void KeyboardMessageReceivedDelegate(int vkCode, KeyboardMessage message);
        public event KeyboardMessageReceivedDelegate? KeyboardMessageReceived;

        private IntPtr _mainWindowHandle = IntPtr.Zero;
        private IntPtr _keyboardHook = IntPtr.Zero;
        private MessageProc _keyboardMessageProc;

        private static readonly Lazy<NativeMethods> ev = new Lazy<NativeMethods>(() => new NativeMethods());
        public static NativeMethods Instance { get { return ev.Value; } }
        public static bool Initialized { get { return ev.IsValueCreated; } }

        protected string _logPrefix;
        protected string _logPrefixBase;

        // When you don't want the ProcessId, use this overload and pass IntPtr.Zero for the second parameter
        [DllImport("user32.dll")]
        internal static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        public bool InstallInputHooks()
        {
            m_GlobalHook = Hook.GlobalEvents();
            return true;

            //if (_keyboardHook == IntPtr.Zero)
            //{
            //    _lastVirtualKeyCode = null;
            //    _lastKeyboardMessage = null;

            //    if (_mainWindowHandle == IntPtr.Zero)
            //    {
            //        _mainWindowHandle = Process.GetCurrentProcess().MainWindowHandle;
            //        LogDebug($"InstallInputHooks", $"MainWindowTitle: {Process.GetCurrentProcess().MainWindowTitle}");
            //        LogDebug($"InstallInputHooks", $"MainWindowHandle: {_mainWindowHandle.ToInt64():X} (Foreground: {GetForegroundWindow() == _mainWindowHandle})");
            //    }

            //    try
            //    {
            //        // Process.GetCurrentProcess().MainModule!.BaseAddress
            //        //var module = Process.GetCurrentProcess().Modules.Cast<ProcessModule>().FirstOrDefault(m => m.ModuleName == "sezzui.dll");
            //        //LogDebug($"InstallInputHooks", $"ffxiv_dx11: {(module != null ? module.BaseAddress.ToInt64() : IntPtr.Zero):X}");
            //        //LogDebug($"InstallInputHooks", $"MainModule: {Process.GetCurrentProcess().MainModule!.BaseAddress:X}");
            //        //uint threadID = GetWindowThreadProcessId(_mainWindowHandle, out uint processHandle);
            //        //LogDebug($"InstallInputHooks", $"threadID: {threadID} {processHandle})");

            //        //var hInst = Marshal.GetHINSTANCE(Assembly.GetExecutingAssembly().GetModules()[0]);
            //        //LogDebug($"InstallInputHooks", $"hInst: {hInst:X}");
            //        //LogDebug($"InstallInputHooks", $"Name: {Assembly.GetExecutingAssembly().GetModules()[0].FullyQualifiedName}");

            //        //_keyboardHook = SetWindowsHookEx(WH_KEYBOARD_LL, _keyboardMessageProc, IntPtr.Zero, 0);
            //        _keyboardHook = SetWindowsHookEx(WH_KEYBOARD_LL, _keyboardMessageProc, IntPtr.Zero, 0);
            //        LogDebug($"InstallInputHooks", $"WH_KEYBOARD_LL (ptr = {_keyboardHook.ToInt64():X})");

            //        int error = Marshal.GetLastWin32Error();
            //        if (error != 0)
            //        {
            //            Win32Exception ex = new(Marshal.GetLastWin32Error());
            //            LogError(ex, $"InstallInputHooks", $"Failed to setup WH_KEYBOARD_LL hook: {ex}");
            //        }
            //    }
            //    catch (Exception ex)
            //    {
            //        LogError(ex, "InstallInputHooks", $"Error: {ex}");
            //    }
            //}
            //else
            //{
            //    LogDebug("InstallInputHooks", "WH_KEYBOARD_LL is already hooked!");
            //}

            //return _keyboardHook != IntPtr.Zero;
        }

        public bool UninstallInputHooks()
        {
            m_GlobalHook?.Dispose();
            return true;

            //if (_keyboardHook != IntPtr.Zero)
            //{
            //    LogDebug("UninstallInputHooks", "WH_KEYBOARD_LL");

            //    try
            //    {
            //        if (UnhookWindowsHookEx(_keyboardHook))
            //        {
            //            _keyboardHook = IntPtr.Zero;
            //            return true;
            //        }
            //        else
            //        {
            //            _keyboardHook = IntPtr.Zero; // It won't work even if we try again.
            //            int error = Marshal.GetLastWin32Error();
            //            if (error != 0)
            //            {
            //                Win32Exception ex = new(Marshal.GetLastWin32Error());
            //                LogError(ex, $"UninstallInputHooks", $"Failed to remove WH_KEYBOARD_LL hook: {ex}");
            //            }
            //            else
            //            {
            //                LogError($"InstallInputHooks", $"Failed to setup WH_KEYBOARD_LL hook.");
            //            }
            //        }
            //    }
            //    catch (Exception ex)
            //    {
            //        LogError(ex, "InstallInputHooks", $"Error: {ex}");
            //        return false;
            //    }
            //}
            //else
            //{
            //    return true;
            //}

            //return false;
        }

        private int? _lastVirtualKeyCode;
        private KeyboardMessage? _lastKeyboardMessage;

        private IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && GetForegroundWindow() == _mainWindowHandle)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                //LogDebug("LowLevelKeyboardProc", $"VKCode {vkCode} Message {(KeyboardMessage)wParam}");

                if (_lastVirtualKeyCode == null || _lastKeyboardMessage == null || _lastVirtualKeyCode != vkCode || _lastKeyboardMessage != (KeyboardMessage)wParam)
                {
                    // Key changed (we don't care about repeated messages while holding a key)
                    _lastVirtualKeyCode = vkCode;
                    _lastKeyboardMessage = (KeyboardMessage)wParam;
                    KeyboardMessageReceived?.Invoke(vkCode, (KeyboardMessage)wParam);
                }

                //KBDLLHOOKSTRUCT? kbd = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
                //if (kbd != null)
                //{
                //    if (_lastVKCode == null || _lastKeyboardMessage == null || _lastVKCode != kbd.vkCode || _lastKeyboardMessage != (KeyboardMessage)wParam) {
                //        // Key changed (we don't care about repeated messages while holding a key)
                //        NewKeyboardMessage?.Invoke(this, new NewKeyboardMessageEventArgs(kbd.vkCode, (KeyboardMessage)wParam));
                //    }
                //}
            }

            return CallNextHookEx(_keyboardHook, nCode, wParam, lParam);
        }

        protected NativeMethods()
        {
            _logPrefixBase = GetType().Name;
            _logPrefix = new StringBuilder("[").Append(_logPrefixBase).Append("] ").ToString();

            _keyboardMessageProc = new MessageProc(LowLevelKeyboardProc);
        }

        ~NativeMethods()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }

            UninstallInputHooks();
        }

        #region Logging
        protected void LogDebug(string messageTemplate, params object[] values)
        {
#if DEBUG
            PluginLog.Debug(new StringBuilder(_logPrefix).Append(messageTemplate).ToString(), values);
#endif
        }

        protected void LogDebug(string messagePrefix, string messageTemplate, params object[] values)
        {
#if DEBUG
            PluginLog.Debug(new StringBuilder("[").Append(_logPrefixBase).Append("::").Append(messagePrefix).Append("] ").Append(messageTemplate).ToString(), values);
#endif
        }

        protected void LogDebug(Exception exception, string messageTemplate, params object[] values)
        {
#if DEBUG
            PluginLog.Debug(exception, new StringBuilder(_logPrefix).Append(messageTemplate).ToString(), values);
#endif
        }

        protected void LogDebug(Exception exception, string messagePrefix, string messageTemplate, params object[] values)
        {
#if DEBUG
            PluginLog.Debug(exception, new StringBuilder("[").Append(_logPrefixBase).Append("::").Append(messagePrefix).Append("] ").Append(messageTemplate).ToString(), values);
#endif
        }

        protected void LogError(string messageTemplate, params object[] values)
        {
            PluginLog.Error(new StringBuilder(_logPrefix).Append(messageTemplate).ToString(), values);
        }

        protected void LogError(string messagePrefix, string messageTemplate, params object[] values)
        {
            PluginLog.Error(new StringBuilder("[").Append(_logPrefixBase).Append("::").Append(messagePrefix).Append("] ").Append(messageTemplate).ToString(), values);
        }

        protected void LogError(Exception exception, string messageTemplate, params object[] values)
        {
            PluginLog.Error(exception, new StringBuilder(_logPrefix).Append(messageTemplate).ToString(), values);
        }

        protected void LogError(Exception exception, string messagePrefix, string messageTemplate, params object[] values)
        {
            PluginLog.Error(exception, new StringBuilder("[").Append(_logPrefixBase).Append("::").Append(messagePrefix).Append("] ").Append(messageTemplate).ToString(), values);
        }
        #endregion
    }
}
