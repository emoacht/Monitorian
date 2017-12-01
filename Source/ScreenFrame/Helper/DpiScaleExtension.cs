using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ScreenFrame.Helper
{
	/// <summary>
	/// Extension methods for <see cref="System.Windows.DpiScale"/>
	/// </summary>
	internal static class DpiScaleExtension
	{
		public static bool IsDefault(this DpiScale a) =>
			(1D == a.DpiScaleX) && (1D == a.DpiScaleY);

		public static bool IsValid(this DpiScale a) =>
			(0 < a.DpiScaleX) && (0 < a.DpiScaleY);
	}
}