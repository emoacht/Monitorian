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
		/// Converts from wParam of WM_DPICHANGED message to DpiScale.
		/// </summary>
		/// <param name="wParam">wParam</param>
		/// <returns>DPI information</returns>
		public static DpiScale FromUInt(uint wParam)
		{
			var dpiX = (ushort)(wParam & 0xffff);
			var dpiY = (ushort)(wParam >> 16);

			return new DpiScale(dpiX / DefaultPixelsPerInch, dpiY / DefaultPixelsPerInch);
		}
	}
}