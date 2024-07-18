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
	/// Gets the current location of cursor.
	/// </summary>
	/// <returns>Location of cursor</returns>
	public static Point GetCursorLocation()
	{
		return TryGetCursorLocation(out POINT location)
			? location
			: default(Point); // (0, 0)
	}

	internal static bool TryGetCursorLocation(out POINT location)
	{
		return GetCursorPos(out location);
	}
}