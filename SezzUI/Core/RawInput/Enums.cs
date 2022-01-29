namespace SezzUI.NativeMethods
{
	public enum KeyState
	{
		KeyDown = 0x0100, // WM_KEYDOWN
		KeyUp = 0x0101, // WM_KEYUP
		SysKeyDown = 0x0104, // WM_SYSKEYDOWN
		SysKeyUp = 0x0105 // WM_SYSKEYUP
	}
}