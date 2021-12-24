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
		public static bool IsDefault(this DpiScale a) => a is { DpiScaleX: 1D, DpiScaleY: 1D };

		public static bool IsValid(this DpiScale a) => a is { DpiScaleX: > 0, DpiScaleY: > 0 };

		public static Matrix ToMatrix(this DpiScale a)
		{
			var matrix = Matrix.Identity;
			matrix.Scale(a.DpiScaleX, a.DpiScaleY);
			return matrix;
		}
	}
}