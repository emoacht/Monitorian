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
	public static Point? GetCursorLocation()
	{
		return GetCursorPos(out POINT buffer) ? buffer : null;
	}

	/// <summary>
	/// Attemps to get the current location of cursor.
	/// </summary>
	/// <param name="location">Location of cursor</param>
	/// <returns>True if successfully gets</returns>
	public static bool TryGetCursorLocation(out Point location)
	{
		if (GetCursorPos(out POINT buffer))
		{
			location = buffer;
			return true;
		}
		location = default;
		return false;
	}
}