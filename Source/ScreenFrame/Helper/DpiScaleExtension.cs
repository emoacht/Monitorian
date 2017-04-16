using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ScreenFrame.Helper
{
	/// <summary>
	/// DpiScale extension methods
	/// </summary>
	internal static class DpiScaleExtension
	{
		private const double DefaultPixelsPerInch = 96D; // Default pixels per Inch

		public static bool IsDefault(this DpiScale a) =>
			(1D == a.DpiScaleX) && (1D == a.DpiScaleY);

		public static bool IsValid(this DpiScale a) =>
			(0 < a.DpiScaleX) && (0 < a.DpiScaleY);

		/// <summary>
		/// Converts WM_DPICHANGED message's wParam value to DpiScale.
		/// </summary>
		/// <param name="wParam">wParam value</param>
		/// <returns>DPI information</returns>
		public static DpiScale FromIntPtr(IntPtr wParam)
		{
			var buff = (uint)wParam;
			var dpiX = (ushort)(buff & 0xffff);
			var dpiY = (ushort)(buff >> 16);

			return new DpiScale(dpiX / DefaultPixelsPerInch, dpiY / DefaultPixelsPerInch);
		}
	}
}