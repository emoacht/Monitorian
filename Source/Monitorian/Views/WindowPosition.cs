using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;

namespace Monitorian.Views
{
	public static class WindowPosition
	{
		#region Win32

		[DllImport("User32.dll", SetLastError = true)]
		private static extern IntPtr FindWindowEx(
			IntPtr hwndParent,
			IntPtr hwndChildAfter,
			string lpszClass,
			string lpszWindow);

		[DllImport("User32.dll", SetLastError = true)]
		private static extern bool GetWindowRect(
			IntPtr hWnd,
			out RECT lpRect);

		[DllImport("User32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool SetWindowPos(
			IntPtr hWnd,
			IntPtr hWndInsertAfter,
			int X,
			int Y,
			int cx,
			int cy,
			SWP uFlags);

		public enum SWP : uint
		{
			SWP_ASYNCWINDOWPOS = 0x4000,
			SWP_DEFERERASE = 0x2000,
			SWP_DRAWFRAME = 0x0020,
			SWP_FRAMECHANGED = 0x0020,
			SWP_HIDEWINDOW = 0x0080,
			SWP_NOACTIVATE = 0x0010,
			SWP_NOCOPYBITS = 0x0100,
			SWP_NOMOVE = 0x0002,
			SWP_NOOWNERZORDER = 0x0200,
			SWP_NOREDRAW = 0x0008,
			SWP_NOREPOSITION = 0x0200,
			SWP_NOSENDCHANGING = 0x0400,
			SWP_NOSIZE = 0x0001,
			SWP_NOZORDER = 0x0004,
			SWP_SHOWWINDOW = 0x0040,
		}

		[DllImport("Dwmapi.dll", SetLastError = true)]
		private static extern int DwmGetWindowAttribute(
			IntPtr hwnd,
			uint dwAttribute,
			out RECT pvAttribute, // IntPtr
			uint cbAttribute);

		[DllImport("Dwmapi.dll", SetLastError = true)]
		private static extern int DwmSetWindowAttribute(
			IntPtr hwnd,
			uint dwAttribute,
			ref bool pvAttribute, // IntPtr
			uint cbAttribute);

		private enum DWMWA : uint
		{
			DWMWA_NCRENDERING_ENABLED = 1,     // [get] Is non-client rendering enabled/disabled
			DWMWA_NCRENDERING_POLICY,          // [set] Non-client rendering policy
			DWMWA_TRANSITIONS_FORCEDISABLED,   // [set] Potentially enable/forcibly disable transitions
			DWMWA_ALLOW_NCPAINT,               // [set] Allow contents rendered in the non-client area to be visible on the DWM-drawn frame.
			DWMWA_CAPTION_BUTTON_BOUNDS,       // [get] Bounds of the caption button area in window-relative space.
			DWMWA_NONCLIENT_RTL_LAYOUT,        // [set] Is non-client content RTL mirrored
			DWMWA_FORCE_ICONIC_REPRESENTATION, // [set] Force this window to display iconic thumbnails.
			DWMWA_FLIP3D_POLICY,               // [set] Designates how Flip3D will treat the window.
			DWMWA_EXTENDED_FRAME_BOUNDS,       // [get] Gets the extended frame bounds rectangle in screen space
			DWMWA_HAS_ICONIC_BITMAP,           // [set] Indicates an available bitmap when there is no better thumbnail representation.
			DWMWA_DISALLOW_PEEK,               // [set] Don't invoke Peek on the window.
			DWMWA_EXCLUDED_FROM_PEEK,          // [set] LivePreview exclusion information
			DWMWA_CLOAK,                       // [set] Cloak or uncloak the window
			DWMWA_CLOAKED,                     // [get] Gets the cloaked state of the window
			DWMWA_FREEZE_REPRESENTATION,       // [set] Force this window to freeze the thumbnail without live update
			DWMWA_LAST
		}

		[DllImport("Shell32.dll", SetLastError = true)]
		private static extern IntPtr SHAppBarMessage(
			ABM dwMessage,
			ref APPBARDATA pData);

		[StructLayout(LayoutKind.Sequential)]
		private struct APPBARDATA
		{
			public uint cbSize;
			public IntPtr hWnd;
			public uint uCallbackMessage;
			public ABE uEdge;
			public RECT rc;
			public int lParam;
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct RECT
		{
			public int left;
			public int top;
			public int right;
			public int bottom;

			public static implicit operator Rect(RECT rect)
			{
				if ((rect.right - rect.left < 0) || (rect.bottom - rect.top < 0))
					return Rect.Empty;

				return new Rect(
					rect.left,
					rect.top,
					rect.right - rect.left,
					rect.bottom - rect.top);
			}
		}

		private enum ABM : uint
		{
			ABM_NEW = 0x00000000,
			ABM_REMOVE = 0x00000001,
			ABM_QUERYPOS = 0x00000002,
			ABM_SETPOS = 0x00000003,
			ABM_GETSTATE = 0x00000004,
			ABM_GETTASKBARPOS = 0x00000005,
			ABM_ACTIVATE = 0x00000006,
			ABM_GETAUTOHIDEBAR = 0x00000007,
			ABM_SETAUTOHIDEBAR = 0x00000008,
			ABM_WINDOWPOSCHANGE = 0x00000009,
			ABM_SETSTATE = 0x0000000A,
		}

		private enum ABE : uint
		{
			ABE_LEFT = 0,
			ABE_TOP = 1,
			ABE_RIGHT = 2,
			ABE_BOTTOM = 3
		}

		[DllImport("Shell32.dll", SetLastError = true)]
		private static extern int Shell_NotifyIconGetRect(
			[In] ref NOTIFYICONIDENTIFIER identifier,
			out RECT iconLocation);

		[StructLayout(LayoutKind.Sequential)]
		private struct NOTIFYICONIDENTIFIER
		{
			public uint cbSize;
			public IntPtr hWnd;
			public uint uID;
			public GUID guidItem; // System.Guid can be used.
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct GUID
		{
			public uint Data1;
			public ushort Data2;
			public ushort Data3;

			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
			public byte[] Data4;
		}

		private const int S_OK = 0x00000000;
		private const int S_FALSE = 0x00000001;

		[StructLayout(LayoutKind.Sequential)]
		public struct WINDOWPOS
		{
			public IntPtr hwnd;
			public IntPtr hwndInsertAfter;
			public int x;
			public int y;
			public int cx;
			public int cy;
			public SWP flags;
		}

		#endregion

		#region Window

		public static bool SetWindowLocation(Window window, Point location)
		{
			var windowHandle = new WindowInteropHelper(window).Handle;

			return SetWindowPos(
				windowHandle,
				new IntPtr(-1), // HWND_TOPMOST
				(int)location.X,
				(int)location.Y,
				0,
				0,
				SWP.SWP_NOSIZE);
		}

		public static Rect GetWindowRect(Window window)
		{
			var windowHandle = new WindowInteropHelper(window).Handle;
			RECT rect;

			if (!GetWindowRect(
				windowHandle,
				out rect))
			{
				return Rect.Empty;
			}

			return rect;
		}

		public static Rect GetDwmWindowRect(Window window)
		{
			var windowHandle = new WindowInteropHelper(window).Handle;
			RECT rect;

			if (DwmGetWindowAttribute(
				windowHandle,
				(uint)DWMWA.DWMWA_EXTENDED_FRAME_BOUNDS,
				out rect,
				(uint)Marshal.SizeOf<RECT>()) != S_OK)
			{
				return Rect.Empty;
			}

			return rect;
		}

		public static Padding GetDwmWindowMargin(Window window)
		{
			var windowHandle = new WindowInteropHelper(window).Handle;
			RECT baseRect;

			if (!GetWindowRect(
				windowHandle,
				out baseRect))
			{
				return Padding.Empty;
			}

			RECT dwmRect;

			if (DwmGetWindowAttribute(
				windowHandle,
				(uint)DWMWA.DWMWA_EXTENDED_FRAME_BOUNDS,
				out dwmRect,
				(uint)Marshal.SizeOf<RECT>()) != S_OK)
			{
				return Padding.Empty;
			}

			return new Padding(
				dwmRect.left - baseRect.left,
				dwmRect.top - baseRect.top,
				baseRect.right - dwmRect.right,
				baseRect.bottom - dwmRect.bottom);
		}

		public static bool DisableTransitions(Window window)
		{
			var windowHandle = new WindowInteropHelper(window).Handle;
			bool value = true;

			if (DwmSetWindowAttribute(
				windowHandle,
				(uint)DWMWA.DWMWA_TRANSITIONS_FORCEDISABLED,
				ref value,
				(uint)Marshal.SizeOf<bool>()) != S_OK)
			{
				return false;
			}
			return true;
		}

		#endregion

		#region Taskbar

		public static bool TryGetTaskbar(out Rect taskbarRect, out TaskbarAlignment taskbarAlignment)
		{
			var data = new APPBARDATA { cbSize = (uint)Marshal.SizeOf(typeof(APPBARDATA)) };

			if (SHAppBarMessage(
				ABM.ABM_GETTASKBARPOS,
				ref data) == IntPtr.Zero)
			{
				taskbarRect = Rect.Empty;
				taskbarAlignment = TaskbarAlignment.None;
				return false;
			}

			taskbarRect = new Rect(data.rc.left, data.rc.top, data.rc.right - data.rc.left, data.rc.bottom - data.rc.top);
			taskbarAlignment = ConvertToTaskbarAlignment(data.uEdge);
			return true;
		}

		public static TaskbarAlignment GetTaskbarAlignment()
		{
			var data = new APPBARDATA { cbSize = (uint)Marshal.SizeOf(typeof(APPBARDATA)) };

			if (SHAppBarMessage(
				ABM.ABM_GETTASKBARPOS,
				ref data) == IntPtr.Zero)
				return TaskbarAlignment.None;

			return ConvertToTaskbarAlignment(data.uEdge);
		}

		private static TaskbarAlignment ConvertToTaskbarAlignment(ABE value)
		{
			switch (value)
			{
				case ABE.ABE_LEFT:
					return TaskbarAlignment.Left;
				case ABE.ABE_TOP:
					return TaskbarAlignment.Top;
				case ABE.ABE_RIGHT:
					return TaskbarAlignment.Right;
				case ABE.ABE_BOTTOM:
					return TaskbarAlignment.Bottom;
				default:
					throw new NotSupportedException("The value is unknown.");
			}
		}

		public static Rect GetTaskbarRect()
		{
			var taskbarHandle = FindWindowEx(
				IntPtr.Zero,
				IntPtr.Zero,
				"Shell_TrayWnd",
				string.Empty);
			if (taskbarHandle == IntPtr.Zero)
				return Rect.Empty;

			RECT taskbarRect;
			if (!GetWindowRect(
				taskbarHandle,
				out taskbarRect))
				return Rect.Empty;

			return taskbarRect;
		}

		#endregion

		#region NotifyIcon

		/// <summary>
		/// Gets the rectangle of a specified NotifyIcon.
		/// </summary>
		/// <param name="notifyIcon">NotifyIcon</param>
		/// <returns>Rectangle of the NotifyIcon</returns>
		/// <remarks>
		/// The idea to get the rectangle of a NotifyIcon is derived from:
		/// https://github.com/rzhw/SuperNotifyIcon
		/// </remarks>
		public static Rect GetNotifyIconRect(NotifyIcon notifyIcon)
		{
			NOTIFYICONIDENTIFIER identifier;
			if (!TryGetNotifyIconIdentifier(notifyIcon, out identifier))
				return Rect.Empty;

			RECT iconLocation;
			int result = Shell_NotifyIconGetRect(ref identifier, out iconLocation);

			switch (result)
			{
				case S_OK:
				case S_FALSE:
					return iconLocation;
				default:
					return Rect.Empty;
			}
		}

		private static bool TryGetNotifyIconIdentifier(NotifyIcon notifyIcon, out NOTIFYICONIDENTIFIER identifier)
		{
			identifier = new NOTIFYICONIDENTIFIER { cbSize = (uint)Marshal.SizeOf(typeof(NOTIFYICONIDENTIFIER)) };

			int id;
			if (!TryGetNonPublicFieldValue(notifyIcon, "id", out id))
				return false;

			NativeWindow window;
			if (!TryGetNonPublicFieldValue(notifyIcon, "window", out window))
				return false;

			identifier.uID = (uint)id;
			identifier.hWnd = window.Handle;
			return true;
		}

		private static bool TryGetNonPublicFieldValue<T>(object instance, string fieldName, out T fieldValue)
		{
			fieldValue = default(T);

			var fieldInfo = instance.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
			if (fieldInfo == null)
				return false;

			var value = fieldInfo.GetValue(instance);
			if (!(value is T))
				return false;

			fieldValue = (T)value;
			return true;
		}

		#endregion
	}

	public enum TaskbarAlignment
	{
		None = 0,
		Left,
		Top,
		Right,
		Bottom,
	}
}