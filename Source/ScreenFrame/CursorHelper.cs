using System.Runtime.InteropServices;
using System.Windows;

using static ScreenFrame.WindowHelper;

namespace ScreenFrame;

/// <summary>
/// Utility methods for cursor
/// </summary>
public static class CursorHelper
{
	#region Win32

	[DllImport("User32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool GetCursorPos(out POINT lpPoint);

	#endregion

	/// <summary>
	/// Gets the current point of cursor.
	/// </summary>
	/// <returns>The point of cursor</returns>
	public static Point GetCursorPoint()
	{
		return TryGetCursorPoint(out POINT point)
			? point
			: default(Point); // (0, 0)
	}

	internal static bool TryGetCursorPoint(out POINT point)
	{
		return GetCursorPos(out point);
	}
}