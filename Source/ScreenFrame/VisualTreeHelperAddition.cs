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
using static ScreenFrame.WindowHelper;

namespace ScreenFrame
{
	/// <summary>
	/// Additional methods for <see cref="System.Windows.Media.VisualTreeHelper"/>
	/// </summary>
	public static class VisualTreeHelperAddition
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

		#region DPI

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
			if (visual is null)
				throw new ArgumentNullException(nameof(visual));

			if (!OsVersion.Is81OrNewer)
				return SystemDpi;

			if (OsVersion.Is10Redstone1OrNewer)
				return VisualTreeHelper.GetDpi(visual);

			var source = PresentationSource.FromVisual(visual) as HwndSource;
			if (source is null)
				return SystemDpi;

			return GetDpiWindow(source.Handle);
		}

		/// <summary>
		/// Gets Per-Monitor DPI of the monitor to which the notification area belongs.
		/// </summary>
		/// <returns>DPI information</returns>
		public static DpiScale GetNotificationAreaDpi()
		{
			if (!OsVersion.Is81OrNewer)
				return SystemDpi;

			var taskbarHandle = FindWindowEx(
				IntPtr.Zero,
				IntPtr.Zero,
				"Shell_TrayWnd",
				string.Empty);
			if (taskbarHandle == IntPtr.Zero)
				return SystemDpi;

			var notificationAreaHandle = FindWindowEx(
				taskbarHandle,
				IntPtr.Zero,
				"TrayNotifyWnd",
				string.Empty);
			if (notificationAreaHandle == IntPtr.Zero)
				return SystemDpi;

			return GetDpiWindow(notificationAreaHandle);
		}

		private static DpiScale GetDpiWindow(IntPtr windowHandle)
		{
			var monitorHandle = MonitorFromWindow(
				windowHandle,
				MONITOR_DEFAULTTO.MONITOR_DEFAULTTOPRIMARY);

			return GetDpi(monitorHandle);
		}

		private static DpiScale GetDpi(IntPtr monitorHandle)
		{
			if (monitorHandle == IntPtr.Zero)
				return SystemDpi;

			var result = GetDpiForMonitor(
				monitorHandle,
				MONITOR_DPI_TYPE.MDT_Default,
				out uint dpiX,
				out uint dpiY);
			if (result != S_OK)
				return SystemDpi;

			return new DpiScale(dpiX / DefaultPixelsPerInch, dpiY / DefaultPixelsPerInch);
		}

		/// <summary>
		/// Converts WM_DPICHANGED message's wParam value to DpiScale.
		/// </summary>
		/// <param name="wParam">wParam value</param>
		/// <returns>DPI information</returns>
		public static DpiScale ConvertToDpiScale(IntPtr wParam)
		{
			var dword = (uint)wParam;
			var dpiX = (ushort)(dword & 0xffff);
			var dpiY = (ushort)(dword >> 16);

			return new DpiScale(dpiX / DefaultPixelsPerInch, dpiY / DefaultPixelsPerInch);
		}

		/// <summary>
		/// Converts WM_DPICHANGED message's lParam value to Rect.
		/// </summary>
		/// <param name="lParam">lParam value</param>
		/// <returns>Rectangle</returns>
		public static Rect ConvertToRect(IntPtr lParam)
		{
			return Marshal.PtrToStructure<RECT>(lParam);
		}

		#endregion

		#region VisualTree

		/// <summary>
		/// Attempts to get the first ancestor visual of a specified visual.
		/// </summary>
		/// <typeparam name="T">Type of ancestor visual</typeparam>
		/// <param name="reference">Descendant visual</param>
		/// <param name="ancestor">Ancestor visual</param>
		/// <returns>True if successfully gets</returns>
		public static bool TryGetAncestor<T>(DependencyObject reference, out T ancestor) where T : DependencyObject
		{
			var parent = reference;

			while (parent is not null)
			{
				parent = VisualTreeHelper.GetParent(parent);
				if (parent is T buffer)
				{
					ancestor = buffer;
					return true;
				}
			}

			ancestor = default;
			return false;
		}

		/// <summary>
		/// Attempts to get the first descendant visual of a specified visual.
		/// </summary>
		/// <typeparam name="T">Type of descendant visual</typeparam>
		/// <param name="reference">Ancestor visual</param>
		/// <param name="descendant">Descendant visual</param>
		/// <returns>True if successfully gets</returns>
		public static bool TryGetDescendant<T>(DependencyObject reference, out T descendant) where T : DependencyObject
		{
			var queue = new Queue<DependencyObject>();
			var parent = reference;

			while (parent is not null)
			{
				var count = VisualTreeHelper.GetChildrenCount(parent);
				for (int i = 0; i < count; i++)
				{
					var child = VisualTreeHelper.GetChild(parent, i);
					if (child is T buffer)
					{
						descendant = buffer;
						return true;
					}
					queue.Enqueue(child);
				}

				parent = (0 < queue.Count) ? queue.Dequeue() : null;
			}

			descendant = default;
			return false;
		}

		#endregion
	}
}