# RawInputLight

This is an absolutely minimal wrapper around Win32 RawInpuit in order to get Mouse, KB and Joystick/gamepad input.

It uses a hidden background window to collect the information and works no matter what window has focus.  It is built
for .NET 6 and talks directly to the Win32API.  As such, it  does NOT depened on any Windows desktop framework (WPF, WinForms, etc)

It is pure C# and, for the most part, depends on the CsWin32 code generator to create the PInvoke stubs on the fly.
CsWin32 generates binding code on the fly from the list in NativeMethods.txt
A small number of manual PInvokes are defined in NativeAPI.CS, along with the all native glue code, for when
I couldnt coax PInvoke into generating soemthing I needed.  

On Rider, you may have to clear the caches to get it to update the PInvoke's correctly, especially if you change
NativeMethods.txt

This release is functioning, see the test app for usage.  Callbacks in this version return the HANDLE for the device
reporting data.  You can get the product and manufacture names with the call NativeAPI.GetDeviceNames(<handle>)
The code for getting those names is borrowed from the rawinput-sharp project. (https://github.com/mfakane/rawinput-sharp)

Note that names are cached by handle.  I dont expect handles to change their meaning during program execution, but if
they do you can regenerate the names by manually calling NativeAPI.RefreshDeviceNames()

Note the HID usages provided in the callbacks are the full usage, including the high bits that indicate the usage page.
## Release Notes:
1.1.0 - refactored the code for improved performance and cleanliness.
        Also put more information in DeviceInfo 
        
1.1.1 - Added NativeAPI.GetDevices
        
1.1.2 - Fixed WNDPROC gc issue
        
1.1.3 - Updated Keyboard and Mouse DeviceInfo to create "fake" UsagePage and Usage info
        
1.1.5 - Bug fixes and converted single callbacks to events for multiplexing
        
