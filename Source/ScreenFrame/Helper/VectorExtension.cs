using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ScreenFrame.Helper
{
	/// <summary>
	/// Extension methods for <see cref="System.Windows.Vector"/>
	/// </summary>
	internal static class VectorExtension
	{
		public static Vector Multiply(this Vector a, DpiScale dpi) =>
			new Vector(a.X * dpi.DpiScaleX, a.Y * dpi.DpiScaleY);
	}
}