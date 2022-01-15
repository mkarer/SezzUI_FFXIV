using System;
using System.Drawing;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Devices.HumanInterfaceDevice;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Input;

namespace RawInputLight
{
	public struct DeviceNames
	{
		public string? devPath;
		public string? Manufacturer;
		public string? Product;

		public unsafe DeviceNames(HANDLE devHandle)
		{
			//get device name
			uint nameSize = 0;
			PInvoke.GetRawInputDeviceInfo(devHandle, RAW_INPUT_DEVICE_INFO_COMMAND.RIDI_DEVICENAME,
				IntPtr.Zero.ToPointer(), &nameSize);
			IntPtr nameBuffer = Marshal.AllocHGlobal((int)nameSize * 2);
			PInvoke.GetRawInputDeviceInfo(devHandle, RAW_INPUT_DEVICE_INFO_COMMAND.RIDI_DEVICENAME,
				nameBuffer.ToPointer(), &nameSize);
			string deviceName = Marshal.PtrToStringAuto(nameBuffer) ?? string.Empty;
			Marshal.FreeHGlobal(nameBuffer);
			//get friendly names
			devPath = deviceName.Substring(4).Replace('#', '\\');
			if (devPath.Contains("{")) devPath =
				devPath.Substring(0, devPath.IndexOf('{') - 1);

			var device = CfgMgr32.LocateDevNode(devPath, CfgMgr32.LocateDevNodeFlags.Phantom);

			Manufacturer = CfgMgr32.GetDevNodePropertyString(device, in DevicePropertyKey.DeviceManufacturer);
			Product = CfgMgr32.GetDevNodePropertyString(device, in DevicePropertyKey.DeviceFriendlyName);
			Product ??= CfgMgr32.GetDevNodePropertyString(device, in DevicePropertyKey.Name);
		}

		public DeviceNames(string path, string manufacturer, string product)
		{
			devPath = path;
			Manufacturer = manufacturer;
			Product = product;
		}
	}

	public struct DeviceInfo
	{
		public HANDLE Handle;
		public DeviceNames Names;
		public HIDP_CAPS DeviceCaps;
		public HIDP_BUTTON_CAPS[] ButtonCaps;
		public HIDP_VALUE_CAPS[] ValueCaps;

		public unsafe DeviceInfo(RAWINPUTDEVICELIST dev)
		{
			Handle = dev.hDevice;

			switch (dev.dwType)
			{
				case RID_DEVICE_INFO_TYPE.RIM_TYPEMOUSE:
					//fake HID usage info;
					DeviceCaps = new HIDP_CAPS();
					DeviceCaps.UsagePage = 1;
					DeviceCaps.Usage = (ushort)((uint)HIDDesktopUsages.GenericDesktopMouse &
												 (uint)0x000000FF);
					ButtonCaps = new HIDP_BUTTON_CAPS[0];
					ValueCaps = new HIDP_VALUE_CAPS[0];
					break;
				case RID_DEVICE_INFO_TYPE.RIM_TYPEKEYBOARD:
					//fake HID usage info
					DeviceCaps = new HIDP_CAPS();
					DeviceCaps.UsagePage = 1;
					DeviceCaps.Usage = (ushort)((uint)HIDDesktopUsages.GenericDesktopKeyboard &
												 (uint)0x000000FF);
					ButtonCaps = new HIDP_BUTTON_CAPS[0];
					ValueCaps = new HIDP_VALUE_CAPS[0];
					break;
				case RID_DEVICE_INFO_TYPE.RIM_TYPEHID:
					using (PreparsedData ppd = new PreparsedData(Handle))
					{
						PInvoke.HidP_GetCaps(ppd, out DeviceCaps);
						//get button caps
						var buttonCapsPtr = Marshal.AllocHGlobal(
							sizeof(HIDP_BUTTON_CAPS) * DeviceCaps.NumberInputButtonCaps);
						ushort buttonCapsLength = DeviceCaps.NumberInputButtonCaps;
						var capsLength = DeviceCaps.NumberInputButtonCaps;
						PInvoke.HidP_GetButtonCaps(HIDP_REPORT_TYPE.HidP_Input,
							(HIDP_BUTTON_CAPS*)buttonCapsPtr.ToPointer(), ref buttonCapsLength,
							ppd);
						// save info
						ButtonCaps = new HIDP_BUTTON_CAPS[buttonCapsLength];
						for (int i = 0; i < buttonCapsLength; i++)
						{
							IntPtr recPtr = IntPtr.Add(buttonCapsPtr, i * sizeof(HIDP_BUTTON_CAPS));
							ButtonCaps[i] = Marshal.PtrToStructure<HIDP_BUTTON_CAPS>(recPtr);
						}
						Marshal.FreeHGlobal(buttonCapsPtr);
						// get value caps
						var valueCapsPtr = Marshal.AllocHGlobal(
							sizeof(HIDP_VALUE_CAPS) * DeviceCaps.NumberInputValueCaps);
						ushort valueCapsLength = DeviceCaps.NumberInputValueCaps;
						HIDP_VALUE_CAPS* pValueCaps = (HIDP_VALUE_CAPS*)valueCapsPtr.ToPointer();
						PInvoke.HidP_GetValueCaps(HIDP_REPORT_TYPE.HidP_Input,
							pValueCaps,
							ref valueCapsLength,
							ppd);
						ValueCaps = new HIDP_VALUE_CAPS[valueCapsLength];
						for (int i = 0; i < valueCapsLength; i++)
						{
							IntPtr rec = IntPtr.Add(valueCapsPtr, i * sizeof(HIDP_VALUE_CAPS));
							ValueCaps[i] = Marshal.PtrToStructure<HIDP_VALUE_CAPS>(rec);
						}
						Marshal.FreeHGlobal(valueCapsPtr);
					}
					break;
				default:
					ButtonCaps = new HIDP_BUTTON_CAPS[0];
					ValueCaps = new HIDP_VALUE_CAPS[0];
					DeviceCaps = new HIDP_CAPS();
					break;
			}
			Names = new DeviceNames(Handle);
		}
	}
}
