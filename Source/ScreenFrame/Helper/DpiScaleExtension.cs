using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

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

		public static Matrix ToMatrix(this DpiScale a)
		{
			var matrix = Matrix.Identity;
			matrix.Scale(a.DpiScaleX, a.DpiScaleY);
			return matrix;
		}
	}
}