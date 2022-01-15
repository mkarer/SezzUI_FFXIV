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
using RawInputLight;
using Windows.Win32.Foundation;
using System.Threading;

namespace SezzUI
{
    internal class NativeMethods : IDisposable
    {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DestroyWindow(IntPtr hwnd);

        [DllImport("Kernel32", ExactSpelling = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        public static extern uint GetCurrentThreadId();

        private IntPtr _mainWindowHandle = IntPtr.Zero;

        private static readonly Lazy<NativeMethods> ev = new Lazy<NativeMethods>(() => new NativeMethods());
        public static NativeMethods Instance { get { return ev.Value; } }
        public static bool Initialized { get { return ev.IsValueCreated; } }
        public void Initialize() { }

        protected string _logPrefix;
        protected string _logPrefixBase;

        private NativeAPI.HWND_WRAPPER? _inputWrapper;
        private RawInput? _rawinput;
        private ushort? _lastVirtualKeyCode;
        private KeyState? _lastKeyState;
        private Task? _taskInputMessagePump;
        private CancellationTokenSource _taskCTS;

        public delegate void OnKeyDownDelegate(ushort vkCode);
        public event OnKeyDownDelegate? OnKeyDown;

        public delegate void OnKeyUpDelegate(ushort vkCode);
        public event OnKeyUpDelegate? OnKeyUp;

        // http://asger-p.dk/info/virtualkeycodes.php
        public const ushort VK_CONTROL = 0x11;
        public const ushort VK_MENU = 0x12;
        public const int VK_RMENU = 0xA5;
        public const int VK_LMENU = 0xA4;
        public const int VK_LCONTROL = 0xA2;
        public const int VK_RCONTROL = 0xA3;

        private void OnKeyStateChanged(HANDLE devID, ushort vkCode, KeyState state)
        {
            if (IsGameInForeground && (_lastVirtualKeyCode == null || _lastKeyState == null || _lastVirtualKeyCode != vkCode || _lastKeyState != state))
            {
                // We don't care about repeated messages while holding a key!
                //LogDebug($"Key: 0x{vkCode:X} {vkCode} : {state}");
                _lastVirtualKeyCode = vkCode;
                _lastKeyState = state;

                if (state == KeyState.KeyUp)
                {
                    OnKeyUp?.Invoke(vkCode);
                }
                else if (state == KeyState.KeyDown)
                {
                    OnKeyDown?.Invoke(vkCode);
                }
            }
        }

        private void MessagePump(CancellationToken ct)
        {
            if (_inputWrapper != null && !ct.IsCancellationRequested)
            {
                LogDebug("MessagePump", "Running Task...");
                NativeAPI.MessagePump((NativeAPI.HWND_WRAPPER)_inputWrapper, ct);
                LogDebug("MessagePump", "Done.");
            }
        }

        public bool InstallInputHooks()
        {
            LogDebug("InstallInputHooks", $"Thread ID: {GetCurrentThreadId()}");
            if (_inputWrapper != null || (_taskInputMessagePump != null && _taskInputMessagePump.Status == TaskStatus.Running))
            {
                LogError("InstallInputHooks", "Error: RawInputLight is still active!");
                return true;
            }

            LogDebug("InstallInputHooks", "Initializing RawInputLight...");

            try
            {
                _inputWrapper = NativeAPI.OpenWindow();
                _rawinput = new RawInput((NativeAPI.HWND_WRAPPER)_inputWrapper);
            }
            catch (Exception ex)
            {
                LogError(ex, "InstallInputHooks", $"Error: {ex}");
            }

            if (_inputWrapper != null && _rawinput != null)
            {
                _rawinput.KeyStateChangeEvent += OnKeyStateChanged;
                _taskInputMessagePump = Task.Run(() => MessagePump(_taskCTS.Token), _taskCTS.Token);
                LogDebug("InstallInputHooks", $"Task started, ID: {_taskInputMessagePump.Id}!");
                //LogDebug("InstallInputHooks", $"Done {_taskCTS.Token.CanBeCanceled}");
                return true;
            }
            else
            {
                LogError("InstallInputHooks", "Failed to setup RawInputLight!");
                return false;
            }
        }

        public void UninstallInputHooks()
        {
            if (_rawinput != null)
            {
                _rawinput.KeyStateChangeEvent -= OnKeyStateChanged;
            }

            if (_taskInputMessagePump != null && _taskInputMessagePump.Status == TaskStatus.Running)
            {
                LogDebug("UninstallInputHooks", $"Trying to cancel RawInputLight task #{_taskInputMessagePump.Id}...");
                _taskCTS.Cancel();

                try
                {
                    _taskInputMessagePump.Wait(_taskCTS.Token);
                }
                catch (OperationCanceledException ex)
                {
                    LogDebug("UninstallInputHooks", $"RawInputLight task has been canceled. ({ex.Message})");
                }
                catch (Exception ex)
                {
                    LogError(ex, "UninstallInputHooks", $"Error cancelling RawInputLight task: {ex}");
                }
                finally
                {
                    _taskCTS.Dispose();
                    LogDebug("UninstallInputHooks", $"Done.");
                    _taskCTS = new();
                }
            }

            if (_inputWrapper != null)
            {
                try
                {
                    if (NativeAPI.CloseWindow((NativeAPI.HWND_WRAPPER)_inputWrapper))
                    {
                        LogDebug("UninstallInputHooks", $"RawInputLight window successfully destroyed.");
                        _inputWrapper = null;
                    }
                    else
                    {
                        LogError("UninstallInputHooks", $"Failed to destroy RawInputLight window.");
                    }
                }
                catch (Exception ex)
                {
                    LogError(ex, "UninstallInputHooks", $"Error destroying RawInputLight window: {ex}");
                }
            }

            _taskInputMessagePump = null;
            _rawinput = null;
        }

        public bool IsGameInForeground => GetForegroundWindow() == _mainWindowHandle;

        protected NativeMethods()
        {
            _logPrefixBase = GetType().Name;
            _logPrefix = new StringBuilder("[").Append(_logPrefixBase).Append("] ").ToString();
            _taskCTS = new();

            _mainWindowHandle = Process.GetCurrentProcess().MainWindowHandle;
            LogDebug($"MainWindowTitle: {Process.GetCurrentProcess().MainWindowTitle}");
            LogDebug($"MainWindowHandle: {_mainWindowHandle.ToInt64():X} (Foreground: {IsGameInForeground})");
            LogDebug($"Thread ID: {GetCurrentThreadId()}");
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
            _taskCTS?.Dispose();
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
