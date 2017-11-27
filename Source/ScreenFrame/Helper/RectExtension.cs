using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ScreenFrame.Helper
{
	/// <summary>
	/// Extension methods for <see cref="System.Windows.Rect"/>
	/// </summary>
	internal static class RectExtension
	{
		/// <summary>
		/// Checks whether specified rectangles are in contact or intersected.
		/// </summary>
		/// <param name="a">Rectangle</param>
		/// <param name="b">Rectangle</param>
		/// <returns>True if in contact or intersected</returns>
		public static bool IsInContactOrIntersected(Rect a, Rect b)
		{
			bool IsInContactOrIntersected(double a1, double a2, double b1, double b2)
			{
				var summedLength = Math.Abs(a1 - a2) + Math.Abs(b1 - b2);
				var array = new[] { a1, a2, b1, b2 };
				var longestDistance = array.Max() - array.Min();

				return (summedLength >= longestDistance);
			}

			return IsInContactOrIntersected(a.Left, a.Right, b.Left, b.Right)
				&& IsInContactOrIntersected(a.Top, a.Bottom, b.Top, b.Bottom);
		}

		/// <summary>
		/// Converts WM_DPICHANGED message's lParam value to Rect.
		/// </summary>
		/// <param name="lParam">lParam value</param>
		/// <returns>Rectangle</returns>
		public static Rect FromIntPtr(IntPtr lParam)
		{
			return Marshal.PtrToStructure<WindowHelper.RECT>(lParam);
		}
	}
}