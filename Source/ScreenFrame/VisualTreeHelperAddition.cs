using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

using ScreenFrame.Helper;

namespace ScreenFrame
{
	internal static class VisualTreeHelperAddition
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

		[DllImport("Gdi32.dll", SetLastError = true)]
		private static extern int GetDeviceCaps(
			IntPtr hdc,
			int nIndex);

		private const int LOGPIXELSX = 88;
		private const int LOGPIXELSY = 90;

		[DllImport("User32.dll", SetLastError = true)]
		private static extern IntPtr GetDC(IntPtr hWnd);

		[DllImport("User32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool ReleaseDC(
			IntPtr hWnd,
			IntPtr hDC);

		[DllImport("Shcore.dll", SetLastError = true)]
		private static extern int GetDpiForMonitor(
			IntPtr hmonitor,
			MONITOR_DPI_TYPE dpiType,
			out uint dpiX,
			out uint dpiY);

		private enum MONITOR_DPI_TYPE
		{
			/// <summary>
			/// Effective DPI that incorporates accessibility overrides and matches what Desktop Window Manage (DWM) uses to scale desktop applications
			/// </summary>
			MDT_Effective_DPI = 0,

			/// <summary>
			/// DPI that ensures rendering at a compliant angular resolution on the screen, without incorporating accessibility overrides
			/// </summary>
			MDT_Angular_DPI = 1,

			/// <summary>
			/// Linear DPI of the screen as measured on the screen itself
			/// </summary>
			MDT_Raw_DPI = 2,

			/// <summary>
			/// Default DPI
			/// </summary>
			MDT_Default = MDT_Effective_DPI
		}

		private const int S_OK = 0x0;

		#endregion

		private const double DefaultPixelsPerInch = 96D; // Default pixels per Inch

		/// <summary>
		/// System DPI
		/// </summary>
		public static DpiScale SystemDpi { get; } = GetSystemDpi();

		private static DpiScale GetSystemDpi()
		{
			var handle = IntPtr.Zero;
			try
			{
				handle = GetDC(IntPtr.Zero);
				if (handle == IntPtr.Zero)
					return new DpiScale(1D, 1D);

				return new DpiScale(
					GetDeviceCaps(handle, LOGPIXELSX) / DefaultPixelsPerInch,
					GetDeviceCaps(handle, LOGPIXELSY) / DefaultPixelsPerInch);
			}
			finally
			{
				if (handle != IntPtr.Zero)
					ReleaseDC(IntPtr.Zero, handle);
			}
		}

		/// <summary>
		/// Gets Per-Monitor DPI of the monitor to which a specified Visual belongs.
		/// </summary>
		/// <param name="visual">Visual</param>
		/// <returns>DPI information</returns>
		public static DpiScale GetDpi(Visual visual)
		{
			if (visual == null)
				throw new ArgumentNullException(nameof(visual));

			if (!OsVersion.Is81OrNewer)
				return SystemDpi;

			if (OsVersion.Is10Redstone1OrNewer)
				return VisualTreeHelper.GetDpi(visual);

			var source = PresentationSource.FromVisual(visual) as HwndSource;
			if (source == null)
				return SystemDpi;

			var handleMonitor = MonitorFromWindow(
				source.Handle,
				MONITOR_DEFAULTTO.MONITOR_DEFAULTTONEAREST);

			return GetDpi(handleMonitor);
		}

		/// <summary>
		/// Gets Per-Monitor DPI of the monitor to which the notification area belongs.
		/// </summary>
		/// <returns>DPI information</returns>
		public static DpiScale GetNotificationAreaDpi()
		{
			if (!OsVersion.Is81OrNewer)
				return SystemDpi;

			var handleTaskBar = FindWindowEx(
				IntPtr.Zero,
				IntPtr.Zero,
				"Shell_TrayWnd",
				string.Empty);
			if (handleTaskBar == IntPtr.Zero)
				return SystemDpi;

			var handleNotificationArea = FindWindowEx(
				handleTaskBar,
				IntPtr.Zero,
				"TrayNotifyWnd",
				string.Empty);
			if (handleNotificationArea == IntPtr.Zero)
				return SystemDpi;

			var handleMonitor = MonitorFromWindow(
				handleNotificationArea,
				MONITOR_DEFAULTTO.MONITOR_DEFAULTTOPRIMARY);

			return GetDpi(handleMonitor);
		}

		private static DpiScale GetDpi(IntPtr handleMonitor)
		{
			if (handleMonitor == IntPtr.Zero)
				return SystemDpi;

			var result = GetDpiForMonitor(
				handleMonitor,
				MONITOR_DPI_TYPE.MDT_Default,
				out uint dpiX,
				out uint dpiY);
			if (result != S_OK)
				return SystemDpi;

			return new DpiScale(dpiX / DefaultPixelsPerInch, dpiY / DefaultPixelsPerInch);
		}
	}
}