using System;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Input;

namespace RawInputLight
{


	public unsafe class PreparsedData : IDisposable
	{
		public IntPtr ppdata;
		public PreparsedData(HANDLE deviceHandle)
		{
			uint dataSize = 0;
			PInvoke.GetRawInputDeviceInfo(
				deviceHandle,
				RAW_INPUT_DEVICE_INFO_COMMAND.RIDI_PREPARSEDDATA,
				IntPtr.Zero.ToPointer(), &dataSize);
			if (dataSize == 0)
			{
				Console.WriteLine("No preparsed data: " + Marshal.GetLastWin32Error());
				return;
			}
			ppdata = Marshal.AllocHGlobal((int)dataSize);
			PInvoke.GetRawInputDeviceInfo(deviceHandle,
				RAW_INPUT_DEVICE_INFO_COMMAND.RIDI_PREPARSEDDATA,
				ppdata.ToPointer(), &dataSize);
			//Ge
		}

		public static implicit operator nint(PreparsedData ppd)
		{
			return (nint)ppd.ppdata.ToInt64();
		}

		public void Dispose()
		{
			Marshal.FreeHGlobal(ppdata);
		}
	}
}