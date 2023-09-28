using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Interop;

namespace ScreenFrame;

/// <summary>
/// Utility methods for <see cref="System.Windows.Window"/>
/// </summary>
public static class WindowHelper
{
	#region Win32

	[DllImport("User32.dll", SetLastError = true)]
	internal static extern IntPtr FindWindowEx(
		IntPtr hwndParent,
		IntPtr hwndChildAfter,
		string lpszClass,
		string lpszWindow);

	[DllImport("User32.dll")]
	internal static extern IntPtr MonitorFromPoint(
		POINT pt,
		MONITOR_DEFAULTTO dwFlags);

	[DllImport("User32.dll")]
	private static extern IntPtr MonitorFromRect(
		ref RECT lprc,
		MONITOR_DEFAULTTO dwFlags);

	[DllImport("User32.dll")]
	internal static extern IntPtr MonitorFromWindow(
		IntPtr hwnd,
		MONITOR_DEFAULTTO dwFlags);

	internal enum MONITOR_DEFAULTTO : uint
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
	private static extern bool GetMonitorInfo(
		IntPtr hMonitor,
		ref MONITORINFO lpmi);

	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
	private struct MONITORINFO
	{
		public uint cbSize;
		public RECT rcMonitor;
		public RECT rcWork;
		public uint dwFlags;
	}

	[StructLayout(LayoutKind.Sequential)]
	internal struct POINT
	{
		public int x;
		public int y;

		public static implicit operator Point(POINT point) => new Point(point.x, point.y);
		public static implicit operator POINT(Point point) => new POINT { x = (int)point.X, y = (int)point.Y };
	}

	[StructLayout(LayoutKind.Sequential)]
	internal struct RECT
	{
		public int left;
		public int top;
		public int right;
		public int bottom;

		public int Width => (right - left);
		public int Height => (bottom - top);

		public static implicit operator Rect(RECT rect)
		{
			return new Rect(
				rect.left,
				rect.top,
				rect.Width,
				rect.Height);
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
	internal struct WINDOWPOS
	{
		public IntPtr hwnd;
		public IntPtr hwndInsertAfter;
		public int x;
		public int y;
		public int cx;
		public int cy;
		public SWP flags;
	}

	[Flags]
	internal enum SWP : uint
	{
		SWP_NOSIZE = 0x0001,
		SWP_NOMOVE = 0x0002,
		SWP_NOZORDER = 0x0004,
		SWP_NOREDRAW = 0x0008,
		SWP_NOACTIVATE = 0x0010,
		SWP_FRAMECHANGED = 0x0020,
		SWP_SHOWWINDOW = 0x0040,
		SWP_HIDEWINDOW = 0x0080,
		SWP_NOCOPYBITS = 0x0100,
		SWP_NOOWNERZORDER = 0x0200,
		SWP_NOSENDCHANGING = 0x0400,

		SWP_DRAWFRAME = 0x0020,
		SWP_NOREPOSITION = 0x0200,

		SWP_DEFERERASE = 0x2000,
		SWP_ASYNCWINDOWPOS = 0x4000,
		SWP_CHANGEWINDOWSTATE = 0x8000 // Undocumented value
	}

	[DllImport("User32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool GetWindowRect(
		IntPtr hWnd,
		out RECT lpRect);

	[DllImport("Dwmapi.dll")]
	private static extern int DwmGetWindowAttribute(
		IntPtr hwnd,
		uint dwAttribute,
		out RECT pvAttribute, // IntPtr
		uint cbAttribute);

	[DllImport("Dwmapi.dll")]
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

	internal const int S_OK = 0x0;
	internal const int S_FALSE = 0x1;

	[DllImport("User32.dll")]
	private static extern IntPtr GetForegroundWindow();

	[DllImport("User32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	internal static extern bool SetForegroundWindow(IntPtr hWnd);

	[DllImport("User32.dll")]
	private static extern uint GetWindowThreadProcessId(
		IntPtr hWnd,
		out uint lpdwProcessId);

	[DllImport("Kernel32.dll")]
	private static extern uint GetCurrentThreadId();

	[DllImport("User32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool AttachThreadInput(
		uint idAttach,
		uint idAttachTo,
		bool fAttach);

	[DllImport("Shell32.dll", SetLastError = true)]
	private static extern uint SHAppBarMessage(
		uint dwMessage,
		ref APPBARDATA pData);

	private const uint ABM_GETSTATE = 0x00000004;
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

	private enum ABS : uint
	{
		ABS_NORMAL = 0x0000000, // Undocumented name
		ABS_AUTOHIDE = 0x0000001,
		ABS_ALWAYSONTOP = 0x0000002
	}

	[DllImport("User32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool EnumWindows(
		EnumWindowsProc lpEnumFunc,
		IntPtr lParam);

	[DllImport("User32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool EnumChildWindows(
		IntPtr hWndParent,
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

	/// <summary>
	/// Attempts to get monitor rectangle which includes a specified point.
	/// </summary>
	/// <param name="point">Point to be checked</param>
	/// <param name="monitorRect">Monitor rectangle</param>
	/// <param name="workRect">Working area rectangle</param>
	/// <returns>True if successfully gets</returns>
	public static bool TryGetMonitorRect(Point point, out Rect monitorRect, out Rect workRect)
	{
		var monitorHandle = MonitorFromPoint(
			point,
			MONITOR_DEFAULTTO.MONITOR_DEFAULTTONULL);

		return TryGetMonitorRect(monitorHandle, out monitorRect, out workRect);
	}

	/// <summary>
	/// Attempts to get monitor rectangle which includes a specified rectangle.
	/// </summary>
	/// <param name="rect">Rectangle to be checked</param>
	/// <param name="monitorRect">Monitor rectangle</param>
	/// <param name="workRect">Working area rectangle</param>
	/// <returns>True if successfully gets</returns>
	public static bool TryGetMonitorRect(Rect rect, out Rect monitorRect, out Rect workRect)
	{
		RECT buffer = rect;
		var monitorHandle = MonitorFromRect(
			ref buffer,
			MONITOR_DEFAULTTO.MONITOR_DEFAULTTONULL);

		return TryGetMonitorRect(monitorHandle, out monitorRect, out workRect);
	}

	private static bool TryGetMonitorRect(IntPtr monitorHandle, out Rect monitorRect, out Rect workRect)
	{
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

	internal static Rect[] GetMonitorRects()
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

	internal static bool SetWindowPosition(Window window, Rect position)
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

	internal static bool TryGetWindowRect(Window window, out Rect windowRect)
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

	internal static bool TryGetDwmWindowRect(Window window, out Rect windowRect)
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

	internal static bool TryGetDwmWindowMargin(Window window, out Thickness windowMargin)
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

	internal static IEnumerable<SWP> EnumerateFlags(SWP flags) =>
		Enum.GetValues(typeof(SWP)).Cast<SWP>().Where(x => (flags & x) == x);

	/// <summary>
	/// Determines if a specified window is foreground.
	/// </summary>
	/// <param name="window">Window to be determined</param>
	/// <returns>True if foreground</returns>
	public static bool IsForegroundWindow(Window window)
	{
		var windowHandle = new WindowInteropHelper(window).Handle;
		return (windowHandle == GetForegroundWindow());
	}

	/// <summary>
	/// Ensures a specified window is foreground.
	/// </summary>
	/// <param name="window">Window to be ensured</param>
	/// <returns>True if foreground</returns>
	public static bool EnsureForegroundWindow(Window window)
	{
		var windowHandle = new WindowInteropHelper(window).Handle;

		var foregroundHandle = GetForegroundWindow();
		if (windowHandle == foregroundHandle)
			return true;

		var currentThreadId = GetCurrentThreadId();
		var foregroundThreadId = GetWindowThreadProcessId(
			foregroundHandle,
			out _);

		try
		{
			if (currentThreadId != foregroundThreadId)
			{
				AttachThreadInput(
					currentThreadId,
					foregroundThreadId,
					true);
			}

			SetForegroundWindow(windowHandle);
		}
		finally
		{
			if (currentThreadId != foregroundThreadId)
			{
				AttachThreadInput(
					currentThreadId,
					foregroundThreadId,
					false);
			}
		}

		return (windowHandle == GetForegroundWindow());
	}

	/// <summary>
	/// Gets all windows.
	/// </summary>
	/// <returns>
	/// <para>parent: Parent window's identifier</para>
	/// <para>child: Child window's identifier</para>
	/// <para>name: Child window's class name</para>
	/// <para>rect: Child window's rectangle</para>
	/// </returns>
	/// <remarks>This method is for research.</remarks>
	public static IReadOnlyList<(int parent, int child, string name, Rect rect)> GetAllWindows()
	{
		var list = new List<(int parent, int child, string name, Rect rect)>();
		int count = 0;

		EnumWindows(
			Proc,
			new IntPtr(count));

		return list.AsReadOnly();

		bool Proc(IntPtr windowHandle, IntPtr lParam)
		{
			var buffer = new StringBuilder(256);

			if (GetClassName(
				windowHandle,
				buffer,
				buffer.Capacity) > 0)
			{
				int parent = lParam.ToInt32();
				int child = ++count;
				var name = buffer.ToString();

				GetWindowRect(
					windowHandle,
					out RECT rect);

				list.Add((parent, child, name, rect));

				EnumChildWindows(
					windowHandle,
					Proc,
					new IntPtr(child));
			}
			return true;
		}
	}

	#endregion

	#region Taskbar

	internal static bool IsTaskbarAutoHide()
	{
		var data = new APPBARDATA { cbSize = (uint)Marshal.SizeOf<APPBARDATA>() };

		var result = SHAppBarMessage(
			ABM_GETSTATE,
			ref data);
		return (result == (uint)ABS.ABS_AUTOHIDE);
	}

	/// <summary>
	/// Attempts to get the information on primary taskbar.
	/// </summary>
	/// <param name="taskbarRect">Primary taskbar rectange</param>
	/// <param name="taskbarAlignment">Primary taskbar alignment</param>
	/// <param name="isShown">Whether primary taskbar is shown or hidden</param>
	/// <returns>True if successfully gets</returns>
	internal static bool TryGetTaskbar(out Rect taskbarRect, out TaskbarAlignment taskbarAlignment, out bool isShown)
	{
		var data = new APPBARDATA { cbSize = (uint)Marshal.SizeOf<APPBARDATA>() };

		var result = SHAppBarMessage(
			ABM_GETTASKBARPOS,
			ref data);
		if (Convert.ToBoolean(result))
		{
			taskbarRect = data.rc;
			taskbarAlignment = ConvertToTaskbarAlignment(data.uEdge);

			if (TryGetWindow(PrimaryTaskbarWindowClassName, out _, out Rect rect))
			{
				// SHAppBarMessage function returns primary taskbar rectangle as if the taskbar
				// is fully shown even when it is actually hidden. In contrast, GetWindowRect
				// function returns actual, current primary taskbar rectangle. Thus, if those
				// rectangles do not match, the taskbar is hidden in full or part.
				isShown = (taskbarRect == rect);
				return true;
			}
		}
		taskbarRect = Rect.Empty;
		taskbarAlignment = default;
		isShown = default;
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

	private static bool TryGetWindow(string className, out IntPtr windowHandle, out Rect windowRect)
	{
		windowHandle = FindWindowEx(
			IntPtr.Zero,
			IntPtr.Zero,
			className,
			null);
		if (windowHandle != IntPtr.Zero)
		{
			if (GetWindowRect(
				windowHandle,
				out RECT rect))
			{
				windowRect = rect;
				return true;
			}
		}
		windowRect = default;
		return false;
	}

	private static bool TryGetChildWindow(string parentClassName, string childClassName, out IntPtr windowHandle, out Rect windowRect)
	{
		windowHandle = FindWindowEx(
			IntPtr.Zero,
			IntPtr.Zero,
			parentClassName,
			null);
		if (windowHandle != IntPtr.Zero)
		{
			windowHandle = FindWindowEx(
				windowHandle,
				IntPtr.Zero,
				childClassName,
				null);
			if (windowHandle != IntPtr.Zero)
			{
				if (GetWindowRect(
					windowHandle,
					out RECT rect))
				{
					windowRect = rect;
					return true;
				}
			}
		}
		windowRect = default;
		return false;
	}

	/// <summary>
	/// Attempts to get the information on primary taskbar.
	/// </summary>
	/// <param name="taskbarRect">Primary taskbar rectangle</param>
	/// <param name="taskbarAlignment">Primary taskbar alignment</param>
	/// <returns>True if successfully gets</returns>
	/// <remarks>If primary taskbar is hidden, this method will fail.</remarks>
	internal static bool TryGetPrimaryTaskbar(out Rect taskbarRect, out TaskbarAlignment taskbarAlignment)
	{
		if (TryGetWindow(PrimaryTaskbarWindowClassName, out IntPtr taskbarHandle, out Rect rect))
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
					// If monitor rectangle intersects with primary taskbar rectangle but
					// does not contain it, the taskbar is hidden in full or part and
					// the monitor to which the taskbar belongs cannot necessarily be
					// determined. It might not be primary monitor because primary taskbar
					// can be placed in monitors other than primary monitor.
					Rect monitorRect = monitorInfo.rcMonitor;
					if (monitorRect.Contains(rect))
					{
						taskbarRect = rect;
						taskbarAlignment = GetTaskbarAlignment(monitorRect, taskbarRect);
						return true;
					}
				}
			}
		}
		taskbarRect = Rect.Empty;
		taskbarAlignment = default;
		return false;
	}

	/// <summary>
	/// Attempts to get the information on secondary taskbar inside a specified monitor rectangle.
	/// </summary>
	/// <param name="monitorRect">Monitor rectangle</param>
	/// <param name="taskbarRect">Secondary taskbar rectangle</param>
	/// <param name="taskbarAlignment">Secondary taskbar alignment</param>
	/// <returns>True if successfully gets</returns>
	/// <remarks>If secondary taskbar is hidden, this method will fail.</remarks>
	internal static bool TryGetSecondaryTaskbar(Rect monitorRect, out Rect taskbarRect, out TaskbarAlignment taskbarAlignment) =>
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
						// If monitor rectangle intersects with taskbar rectangle but
						// does not contain it, the taskbar is hidden in full or part and
						// the monitor to which the taskbar belongs cannot necessarily be
						// determined.
						if (monitorRect.Contains(rect))
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

	/// <summary>
	/// Attempts to get the rectangle of Start button or ToolBar.
	/// </summary>
	/// <param name="buttonRect">Rectangle of Start button or ToolBar</param>
	/// <returns>True if successfully gets</returns>
	internal static bool TryGetStartButtonRect(out Rect buttonRect)
	{
		// As of Windows 11 10.0.22621.xxx, the traditional window of primary taskbar
		// (Shell_TrayWnd) no longer indicates the actual height of primary taskbar. Instead,
		// other windows (Start, ReBarWindow32) can be used to get the actual height.
		if (TryGetChildWindow(PrimaryTaskbarWindowClassName, "Start", out _, out Rect windowRect)
			&& IsValid(windowRect))
		{
			buttonRect = windowRect;
			return true;
		}
		if (TryGetChildWindow(PrimaryTaskbarWindowClassName, "ReBarWindow32", out _, out windowRect)
			&& IsValid(windowRect))
		{
			buttonRect = windowRect;
			return true;
		}
		buttonRect = default;
		return false;
	}

	/// <summary>
	/// Attempts to get the rectangle of overflow area.
	/// </summary>
	/// <param name="overflowAreaRect">Rectangle of overflow area</param>
	/// <param name="isMarginIncluded">Whether rectangle of overflow area includes margin</param>
	/// <returns>True if successfully gets</returns>
	/// <remarks>
	/// This method will not produce current location of overflow area until it is shown
	/// in the monitor where primary taskbar currently locates. Thus, the location must be
	/// verified by other means.
	/// </remarks>
	internal static bool TryGetOverflowAreaRect(out Rect overflowAreaRect, out bool isMarginIncluded)
	{
		// As of Windows 11 10.0.22621.xxx, the traditional window of overflow area
		// (NotifyIconOverflowWindow) still exists but seems no longer used. Instead, another
		// window (TopLevelWindowForOverflowXamlIsland) hosts overflow area. Its rectangle
		// includes margin surrounding it.
		if (TryGetWindow("NotifyIconOverflowWindow", out _, out Rect windowRect)
			&& IsValid(windowRect))
		{
			overflowAreaRect = windowRect;
			isMarginIncluded = false;
			return true;
		}
		if (TryGetWindow("TopLevelWindowForOverflowXamlIsland", out _, out windowRect)
			&& IsValid(windowRect))
		{
			overflowAreaRect = windowRect;
			isMarginIncluded = true;
			return true;
		}
		overflowAreaRect = default;
		isMarginIncluded = false;
		return false;
	}

	private static bool IsValid(Rect rect) => rect is { Width: > 0, Height: > 0 };

	/// <summary>
	/// Attempts to get the information on primary taskbar from <see cref="System.Windows.SystemParameters"/>.
	/// </summary>
	/// <param name="taskbarRect">Primary taskbar rectange</param>
	/// <param name="taskbarAlignment">Primary taskbar alignment</param>
	/// <returns>True if successfully gets</returns>
	/// <remarks>
	/// If primary taskbar is set to auto hide, this method will fail even when primary taskbar
	/// is shown temporarily.
	/// </remarks>
	internal static bool TryGetSystemPrimaryTaskbar(out Rect taskbarRect, out TaskbarAlignment taskbarAlignment)
	{
		var wa = SystemParameters.WorkArea;
		var gapWidth = SystemParameters.PrimaryScreenWidth - wa.Width;
		var gapHeight = SystemParameters.PrimaryScreenHeight - wa.Height;

		(taskbarRect, taskbarAlignment) = (wa.X, wa.Y, gapWidth, gapHeight) switch
		{
			( > 0, 0, > 0, 0) => (Create(0, 0, gapWidth, wa.Height), TaskbarAlignment.Left),
			(0, > 0, 0, > 0) => (Create(0, 0, wa.Width, gapHeight), TaskbarAlignment.Top),
			(0, 0, > 0, 0) => (Create(wa.Width, 0, gapWidth, wa.Height), TaskbarAlignment.Right),
			(0, 0, 0, > 0) => (Create(0, wa.Height, wa.Width, gapHeight), TaskbarAlignment.Bottom),
			_ => default // Auto hide
		};
		return (taskbarAlignment != TaskbarAlignment.None);

		static Rect Create(double x, double y, double width, double height)
		{
			var monitorHandle = MonitorFromWindow(
				IntPtr.Zero,
				MONITOR_DEFAULTTO.MONITOR_DEFAULTTOPRIMARY);

			var dpi = VisualTreeHelperAddition.GetDpiWindow(monitorHandle);

			return new Rect(
				x * dpi.DpiScaleX,
				y * dpi.DpiScaleY,
				width * dpi.DpiScaleX,
				height * dpi.DpiScaleY);
		}
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