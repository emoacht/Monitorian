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

		[DllImport("User32.dll")]
		private static extern IntPtr MonitorFromRect(
			ref RECT lprc,
			MONITOR_DEFAULTTO dwFlags);

		[DllImport("User32.dll")]
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

		[DllImport("User32.dll", CharSet = CharSet.Unicode)]
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
				if ((rect.right < rect.left) || (rect.bottom < rect.top))
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
					left = (int)rect.Left,
					top = (int)rect.Top,
					right = (int)rect.Right,
					bottom = (int)rect.Bottom
				};
			}
		}

		private const int MONITORINFOF_PRIMARY = 0x00000001;

		[DllImport("User32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
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

		[DllImport("User32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool GetWindowRect(
			IntPtr hWnd,
			out RECT lpRect);

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

		public const int S_OK = 0x0;
		public const int S_FALSE = 0x1;

		[DllImport("User32.dll", SetLastError = true)]
		private static extern IntPtr GetForegroundWindow();

		[DllImport("Shell32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool SHAppBarMessage(
			uint dwMessage, // ABM_GETTASKBARPOS
			ref APPBARDATA pData);

		private const uint ABM_GETTASKBARPOS = 0x00000005;

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

		private enum ABE : uint
		{
			ABE_LEFT = 0,
			ABE_TOP = 1,
			ABE_RIGHT = 2,
			ABE_BOTTOM = 3
		}

		[DllImport("User32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool EnumWindows(
			EnumWindowsProc lpEnumFunc,
			IntPtr lParam);

		[return: MarshalAs(UnmanagedType.Bool)]
		private delegate bool EnumWindowsProc(
			IntPtr hWnd,
			IntPtr lParam);

		[DllImport("User32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		private static extern int GetClassName(
			IntPtr hWnd,
			StringBuilder lpClassName,
			int nMaxCount);

		#endregion

		#region Monitor

		public static bool TryGetMonitorRect(Rect windowRect, out Rect monitorRect, out Rect workRect)
		{
			RECT rect = windowRect;
			var monitorHandle = MonitorFromRect(
				ref rect,
				MONITOR_DEFAULTTO.MONITOR_DEFAULTTONULL);
			if (monitorHandle != IntPtr.Zero)
			{
				var monitorInfo = new MONITORINFO { cbSize = (uint)Marshal.SizeOf<MONITORINFO>() };

				if (GetMonitorInfo(
					monitorHandle,
					ref monitorInfo))
				{
					monitorRect = monitorInfo.rcMonitor;
					workRect = monitorInfo.rcWork;
					return true;
				}
			}
			monitorRect = Rect.Empty;
			workRect = Rect.Empty;
			return false;
		}

		public static Rect[] GetMonitorRects()
		{
			var monitorRects = new List<Rect>();

			if (EnumDisplayMonitors(
				IntPtr.Zero,
				IntPtr.Zero,
				Proc,
				IntPtr.Zero))
			{
				return monitorRects.ToArray();
			}
			return Array.Empty<Rect>();

			bool Proc(IntPtr monitorHandle, IntPtr hdcMonitor, IntPtr lprcMonitor, IntPtr dwData)
			{
				var monitorInfo = new MONITORINFO { cbSize = (uint)Marshal.SizeOf<MONITORINFO>() };

				if (GetMonitorInfo(
					monitorHandle,
					ref monitorInfo))
				{
					if (Convert.ToBoolean(monitorInfo.dwFlags & MONITORINFOF_PRIMARY))
					{
						// Store the primary monitor at the beginning of the collection because in
						// most cases, the primary monitor should be checked first.
						monitorRects.Insert(0, monitorInfo.rcMonitor);
					}
					else
					{
						monitorRects.Add(monitorInfo.rcMonitor);
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

			if (GetWindowRect(
				windowHandle,
				out RECT rect))
			{
				windowRect = rect;
				return true;
			}
			windowRect = Rect.Empty;
			return false;
		}

		public static bool TryGetDwmWindowRect(Window window, out Rect windowRect)
		{
			var windowHandle = new WindowInteropHelper(window).Handle;

			if (DwmGetWindowAttribute(
				windowHandle,
				(uint)DWMWA.DWMWA_EXTENDED_FRAME_BOUNDS,
				out RECT rect,
				(uint)Marshal.SizeOf<RECT>()) == S_OK)
			{
				windowRect = rect;
				return true;
			}
			windowRect = Rect.Empty;
			return false;
		}

		public static bool TryGetDwmWindowMargin(Window window, out Thickness windowMargin)
		{
			var windowHandle = new WindowInteropHelper(window).Handle;

			if (GetWindowRect(
				windowHandle,
				out RECT baseRect))
			{
				if (DwmGetWindowAttribute(
					windowHandle,
					(uint)DWMWA.DWMWA_EXTENDED_FRAME_BOUNDS,
					out RECT dwmRect,
					(uint)Marshal.SizeOf<RECT>()) == S_OK)
				{
					windowMargin = new Thickness(
						dwmRect.left - baseRect.left,
						dwmRect.top - baseRect.top,
						baseRect.right - dwmRect.right,
						baseRect.bottom - dwmRect.bottom);
					return true;
				}
			}
			windowMargin = default;
			return false;
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
				ABM_GETTASKBARPOS,
				ref data))
			{
				taskbarRect = data.rc;
				taskbarAlignment = ConvertToTaskbarAlignment(data.uEdge);
				return true;
			}
			taskbarRect = Rect.Empty;
			taskbarAlignment = default;
			return false;

			static TaskbarAlignment ConvertToTaskbarAlignment(ABE value)
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
		}

		// Primary taskbar does not necessarily locate in primary monitor.
		private const string PrimaryTaskbarWindowClassName = "Shell_TrayWnd";
		private const string SecondaryTaskbarWindowClassName = "Shell_SecondaryTrayWnd";

		public static bool TryGetPrimaryTaskbar(out Rect taskbarRect, out TaskbarAlignment taskbarAlignment)
		{
			var taskbarHandle = FindWindowEx(
				IntPtr.Zero,
				IntPtr.Zero,
				PrimaryTaskbarWindowClassName,
				string.Empty);
			if (taskbarHandle != IntPtr.Zero)
			{
				var monitorHandle = MonitorFromWindow(
					taskbarHandle,
					MONITOR_DEFAULTTO.MONITOR_DEFAULTTONULL);
				if (monitorHandle != IntPtr.Zero)
				{
					var monitorInfo = new MONITORINFO { cbSize = (uint)Marshal.SizeOf<MONITORINFO>() };

					if (GetMonitorInfo(
						monitorHandle,
						ref monitorInfo))
					{
						if (GetWindowRect(
							taskbarHandle,
							out RECT rect))
						{
							taskbarRect = rect;
							taskbarAlignment = GetTaskbarAlignment(monitorInfo.rcMonitor, taskbarRect);
							return true;
						}
					}
				}
			}
			taskbarRect = Rect.Empty;
			taskbarAlignment = default;
			return false;
		}

		public static bool TryGetSecondaryTaskbar(Rect monitorRect, out Rect taskbarRect, out TaskbarAlignment taskbarAlignment) =>
			TryGetTaskbar(SecondaryTaskbarWindowClassName, monitorRect, out taskbarRect, out taskbarAlignment);

		private static bool TryGetTaskbar(string className, Rect monitorRect, out Rect taskbarRect, out TaskbarAlignment taskbarAlignment)
		{
			var matchRect = Rect.Empty;

			if (EnumWindows(
				Proc,
				IntPtr.Zero))
			{
				if (matchRect != Rect.Empty)
				{
					taskbarRect = matchRect;
					taskbarAlignment = GetTaskbarAlignment(monitorRect, taskbarRect);
					return true;
				}
			}
			taskbarRect = Rect.Empty;
			taskbarAlignment = default;
			return false;

			bool Proc(IntPtr windowHandle, IntPtr lParam)
			{
				var buffer = new StringBuilder(256);

				if (GetClassName(
					windowHandle,
					buffer,
					buffer.Capacity) > 0)
				{
					if (buffer.ToString() == className)
					{
						if (GetWindowRect(
							windowHandle,
							out RECT rect))
						{
							if (monitorRect.IntersectsWith(rect))
							{
								matchRect = rect;
								return false;
							}
						}
					}
				}
				return true;
			}
		}

		private static TaskbarAlignment GetTaskbarAlignment(Rect monitorRect, Rect taskbarRect)
		{
			return (left: (monitorRect.Left == taskbarRect.Left),
					top: (monitorRect.Top == taskbarRect.Top),
					right: (monitorRect.Right == taskbarRect.Right),
					bottom: (monitorRect.Bottom == taskbarRect.Bottom)) switch
			{
				(true, true, right: false, true) => TaskbarAlignment.Left,
				(true, true, true, bottom: false) => TaskbarAlignment.Top,
				(left: false, true, true, true) => TaskbarAlignment.Right,
				(true, top: false, true, true) => TaskbarAlignment.Bottom,
				_ => default
			};
		}

		public static bool TryGetOverflowAreaRect(out Rect overflowAreaRect)
		{
			// This combination of functions will not produce current location of overflow area
			// until it is shown in the monitor where primary taskbar currently locates. Thus
			// the location must be verified by other means.
			var overflowAreaHandle = FindWindowEx(
				IntPtr.Zero,
				IntPtr.Zero,
				"NotifyIconOverflowWindow",
				string.Empty);
			if (overflowAreaHandle != IntPtr.Zero)
			{
				if (GetWindowRect(
					overflowAreaHandle,
					out RECT rect))
				{
					overflowAreaRect = rect;
					return true;
				}
			}
			overflowAreaRect = Rect.Empty;
			return false;
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