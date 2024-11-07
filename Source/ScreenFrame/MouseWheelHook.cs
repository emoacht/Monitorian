using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ScreenFrame;

/// <summary>
/// Encapsulates a low-level mouse wheel hook.
/// </summary>
class MouseWheelHook
{
	#region Win32
	[StructLayout(LayoutKind.Sequential)]
	internal struct POINT
	{
		public int x;
		public int y;
	}

	[StructLayout(LayoutKind.Sequential)]
	internal struct MouseLLHookStruct
	{
		public POINT pt;
		public int mouseData;
		public int flags;
		public int time;
		public int dwExtraInfo;
	}

	delegate int HookProc(int nCode, IntPtr wParam, IntPtr lParam);

	[DllImport("user32.dll", PreserveSig = true)]
	static extern int SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hInstance, int threadId);

	[DllImport("user32.dll", PreserveSig = true)]
	static extern bool UnhookWindowsHookEx(int idHook);

	[DllImport("user32.dll", PreserveSig = true)]
	static extern int CallNextHookEx(int idHook, int nCode, IntPtr wParam, IntPtr lParam);

	const int WM_MOUSEWHEEL = 0x020A;
	const int WH_MOUSE_LL = 14;
	#endregion

	public delegate int MouseWheelHandler(object sender, MouseEventArgs e);
	public event MouseWheelHandler MouseWheelEvent;

	HookProc _hProc;
	int _hHook;

	public void SetHook()
	{
		if (_hHook == 0)
		{
			_hProc = new HookProc(MouseHookProc);
			_hHook = SetWindowsHookEx(WH_MOUSE_LL, _hProc, IntPtr.Zero, 0);
		}
	}

	public void Unhook()
	{
		if (_hHook != 0)
		{
			UnhookWindowsHookEx(_hHook);
			_hHook = 0;
		}
	}

	int MouseHookProc(int nCode, IntPtr wParam, IntPtr lParam)
	{
		if (nCode < 0 || MouseWheelEvent == null || (int)wParam != WM_MOUSEWHEEL)
			return CallNextHookEx(_hHook, nCode, wParam, lParam);

		var mouseData = Marshal.PtrToStructure<MouseLLHookStruct>(lParam);
		var result = MouseWheelEvent(this, new MouseEventArgs(MouseButtons.None, 0, mouseData.pt.x, mouseData.pt.y, mouseData.mouseData >> 16));
		if (result == 0)
			return CallNextHookEx(_hHook, nCode, wParam, lParam);

		return result;
	}
}
