using System;
using System.Net;
using Windows.Win32.Foundation;

namespace RawInputLight
{
	public class RawInput
	{
		private const ushort GenericDesktopPage = 0x01;
		private const ushort GenericMouse = 0x02;
		private const ushort GenericJoystick = 0x04;
		private const ushort GenericGamepad = 0x05;
		private const ushort GenericKeyboard = 0x06;

		public event Action<HANDLE, ushort, KeyState>? KeyStateChangeEvent;
		public event Action<HANDLE, int, int, uint, int>? MouseStateChangeEvent;
		public event Action<HANDLE, uint, bool[]>? ButtonDownEvent;
		public event Action<HANDLE, uint[], uint[]>? AxisEvent;

		public RawInput(NativeAPI.HWND_WRAPPER wrapper) : this(wrapper.hwnd)
		{
			NativeAPI.KeyListeners += (dev, arg1, state) =>
				KeyStateChangeEvent?.Invoke(dev, arg1, state);
			NativeAPI.MouseStateListeners += (dev, i, i1, arg3, arg4) =>
				MouseStateChangeEvent?.Invoke(dev, i, i1, arg3, arg4);
			NativeAPI.ButtonDownListeners += (dev, usageBase, states) =>
				ButtonDownEvent?.Invoke(dev, usageBase, states);
			NativeAPI.AxisListeners += (dev, usageBase, values) =>
				AxisEvent?.Invoke(dev, usageBase, values);
		}

		public RawInput(HWND windowHandle)
		{
			NativeAPI.RegisterInputDevices(windowHandle, new NativeAPI.HID_DEV_ID[]
			{
			new NativeAPI.HID_DEV_ID(GenericDesktopPage, GenericMouse),
			new NativeAPI.HID_DEV_ID(GenericDesktopPage, GenericGamepad),
			new NativeAPI.HID_DEV_ID(GenericDesktopPage, GenericJoystick),
			new NativeAPI.HID_DEV_ID(GenericDesktopPage, GenericKeyboard)
			});

		}
	}
}
