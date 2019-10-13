using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;

namespace ScreenFrame
{
	internal static class WindowHelper
	{
		#region Win32

		[DllImport("User32.dll", SetLastError = true)]
		private static extern IntPtr FindWindowEx(
			IntPtr hwndParent,
			IntPtr hwndChildAfter,
			string lpszClass,
			string lpszWindow);

		[DllImport("User32.dll", SetLastError = true)]
		private static extern IntPtr MonitorFromWindow(
			IntPtr hwnd,
			MONITOR_DEFAULTTO dwFlags);

		private enum MONITOR_DEFAULTTO : uint
		{
			/// <summary>
			/// If no display monitor intersects, returns null.
			/// </summary>
			MONITOR_DEFAULTTONULL = 0x00000000,

			/// <summary>
			/// If no display monitor intersects, returns a handle to the primary display monitor.
			/// </summary>
			MONITOR_DEFAULTTOPRIMARY = 0x00000001,

			/// <summary>
			/// If no display monitor intersects, returns a handle to the display monitor that is nearest to the rectangle.
			/// </summary>
			MONITOR_DEFAULTTONEAREST = 0x00000002,
		}

		[DllImport("User32.dll")]
		private static extern bool EnumDisplayMonitors(
			IntPtr hdc,
			IntPtr lprcClip,
			MonitorEnumProc lpfnEnum,
			IntPtr dwData);

		[return: MarshalAs(UnmanagedType.Bool)]
		private delegate bool MonitorEnumProc(
			IntPtr hMonitor,
			IntPtr hdcMonitor,
			IntPtr lprcMonitor,
			IntPtr dwData);

		[DllImport("User32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool GetMonitorInfo(
			IntPtr hMonitor,
			ref MONITORINFO lpmi);

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		public struct MONITORINFO
		{
			public uint cbSize;
			public RECT rcMonitor;
			public RECT rcWork;
			public uint dwFlags;
		}

		private const int MONITORINFOF_PRIMARY = 0x00000001;

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
			SWP_CHANGEWINDOWSTATE = 0x8000 // Undocumented value
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
			[In] ref bool pvAttribute, // IntPtr
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

		[DllImport("User32.dll", SetLastError = true)]
		private static extern IntPtr GetForegroundWindow();

		[StructLayout(LayoutKind.Sequential)]
		public struct POINT
		{
			public int x;
			public int y;

			public static implicit operator Point(POINT point) => new Point(point.x, point.y);
			public static implicit operator POINT(Point point) => new POINT { x = (int)point.X, y = (int)point.Y };
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct RECT
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

			public static implicit operator RECT(Rect rect)
			{
				return new RECT
				{
					left = (int)rect.X,
					top = (int)rect.Y,
					right = (int)rect.Right,
					bottom = (int)rect.Bottom
				};
			}
		}

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

		public const int S_OK = 0x0;
		public const int S_FALSE = 0x1;

		#endregion

		#region Monitor

		public static bool TryGetMonitorRect(Window window, out Rect monitorRect)
		{
			var windowHandle = new WindowInteropHelper(window).Handle;

			var monitorHandle = MonitorFromWindow(
				windowHandle,
				MONITOR_DEFAULTTO.MONITOR_DEFAULTTONEAREST);

			var monitorInfo = new MONITORINFO { cbSize = (uint)Marshal.SizeOf<MONITORINFO>() };

			if (!GetMonitorInfo(monitorHandle, ref monitorInfo))
			{
				monitorRect = Rect.Empty;
				return false;
			}
			monitorRect = monitorInfo.rcMonitor;
			return true;
		}

		public static Rect[] GetMonitorRects()
		{
			var holder = new MonitorEnumHolder();

			if (!EnumDisplayMonitors(
				IntPtr.Zero,
				IntPtr.Zero,
				holder.MonitorEnum,
				IntPtr.Zero))
			{
				return Array.Empty<Rect>();
			}
			return holder.MonitorRects.ToArray();
		}

		private class MonitorEnumHolder
		{
			public readonly List<Rect> MonitorRects = new List<Rect>();

			public bool MonitorEnum(IntPtr hMonitor, IntPtr hdcMonitor, IntPtr lprcMonitor, IntPtr dwData)
			{
				var monitorInfo = new MONITORINFO { cbSize = (uint)Marshal.SizeOf<MONITORINFO>() };

				if (GetMonitorInfo(hMonitor, ref monitorInfo))
				{
					if (Convert.ToBoolean(monitorInfo.dwFlags & MONITORINFOF_PRIMARY))
					{
						// Store the primary monitor at the beginning of the collection because in most cases,
						// the primary monitor should be checked first.
						MonitorRects.Insert(0, monitorInfo.rcMonitor);
					}
					else
					{
						MonitorRects.Add(monitorInfo.rcMonitor);
					}
				}
				return true;
			}
		}

		#endregion

		#region Window

		public static bool SetWindowPosition(Window window, Rect position)
		{
			var windowHandle = new WindowInteropHelper(window).Handle;

			return SetWindowPos(
				windowHandle,
				new IntPtr(-1), // HWND_TOPMOST
				(int)position.X,
				(int)position.Y,
				(int)position.Width,
				(int)position.Height,
				SWP.SWP_NOZORDER);
		}

		public static bool TryGetWindowRect(Window window, out Rect windowRect)
		{
			var windowHandle = new WindowInteropHelper(window).Handle;

			if (!GetWindowRect(
				windowHandle,
				out RECT rect))
			{
				windowRect = Rect.Empty;
				return false;
			}
			windowRect = rect;
			return true;
		}

		public static bool TryGetDwmWindowRect(Window window, out Rect windowRect)
		{
			var windowHandle = new WindowInteropHelper(window).Handle;

			if (DwmGetWindowAttribute(
				windowHandle,
				(uint)DWMWA.DWMWA_EXTENDED_FRAME_BOUNDS,
				out RECT rect,
				(uint)Marshal.SizeOf<RECT>()) != S_OK)
			{
				windowRect = Rect.Empty;
				return false;
			}
			windowRect = rect;
			return true;
		}

		public static bool TryGetDwmWindowMargin(Window window, out Thickness windowMargin)
		{
			var windowHandle = new WindowInteropHelper(window).Handle;

			if (!GetWindowRect(
				windowHandle,
				out RECT baseRect))
			{
				windowMargin = default;
				return false;
			}

			if (DwmGetWindowAttribute(
				windowHandle,
				(uint)DWMWA.DWMWA_EXTENDED_FRAME_BOUNDS,
				out RECT dwmRect,
				(uint)Marshal.SizeOf<RECT>()) != S_OK)
			{
				windowMargin = default;
				return false;
			}

			windowMargin = new Thickness(
				dwmRect.left - baseRect.left,
				dwmRect.top - baseRect.top,
				baseRect.right - dwmRect.right,
				baseRect.bottom - dwmRect.bottom);
			return true;
		}

		public static bool DisableTransitions(Window window)
		{
			var windowHandle = new WindowInteropHelper(window).Handle;
			bool value = true;

			return (DwmSetWindowAttribute(
				windowHandle,
				(uint)DWMWA.DWMWA_TRANSITIONS_FORCEDISABLED,
				ref value,
				(uint)Marshal.SizeOf<bool>()) == S_OK);
		}

		public static bool IsForegroundWindow(Window window)
		{
			var windowHandle = new WindowInteropHelper(window).Handle;
			return windowHandle == GetForegroundWindow();
		}

		public static IEnumerable<SWP> EnumerateFlags(SWP flags) =>
			Enum.GetValues(typeof(SWP)).Cast<SWP>().Where(x => flags.HasFlag(x));

		#endregion

		#region Taskbar

		public static bool TryGetTaskbar(out Rect taskbarRect, out TaskbarAlignment taskbarAlignment)
		{
			var data = new APPBARDATA { cbSize = (uint)Marshal.SizeOf<APPBARDATA>() };

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
			var data = new APPBARDATA { cbSize = (uint)Marshal.SizeOf<APPBARDATA>() };

			if (SHAppBarMessage(
				ABM.ABM_GETTASKBARPOS,
				ref data) == IntPtr.Zero)
			{
				return TaskbarAlignment.None;
			}

			return ConvertToTaskbarAlignment(data.uEdge);
		}

		private static TaskbarAlignment ConvertToTaskbarAlignment(ABE value)
		{
			return value switch
			{
				ABE.ABE_LEFT => TaskbarAlignment.Left,
				ABE.ABE_TOP => TaskbarAlignment.Top,
				ABE.ABE_RIGHT => TaskbarAlignment.Right,
				ABE.ABE_BOTTOM => TaskbarAlignment.Bottom,
				_ => throw new NotSupportedException("The value is unknown."),
			};
		}

		public static bool TryGetTaskbarRect(out Rect taskbarRect)
		{
			var taskbarHandle = FindWindowEx(
				IntPtr.Zero,
				IntPtr.Zero,
				"Shell_TrayWnd",
				string.Empty);
			if (taskbarHandle == IntPtr.Zero)
			{
				taskbarRect = Rect.Empty;
				return false;
			}

			if (!GetWindowRect(
				taskbarHandle,
				out RECT rect))
			{
				taskbarRect = Rect.Empty;
				return false;
			}
			taskbarRect = rect;
			return true;
		}

		public static bool TryGetOverflowAreaRect(out Rect overflowAreaRect)
		{
			var overflowAreaHandle = FindWindowEx(
				IntPtr.Zero,
				IntPtr.Zero,
				"NotifyIconOverflowWindow",
				string.Empty);
			if (overflowAreaHandle == IntPtr.Zero)
			{
				overflowAreaRect = Rect.Empty;
				return false;
			}

			if (!GetWindowRect(
				overflowAreaHandle,
				out RECT rect))
			{
				overflowAreaRect = Rect.Empty;
				return false;
			}
			overflowAreaRect = rect;
			return true;
		}

		#endregion
	}

	internal enum TaskbarAlignment
	{
		None = 0,
		Left,
		Top,
		Right,
		Bottom,
	}
}