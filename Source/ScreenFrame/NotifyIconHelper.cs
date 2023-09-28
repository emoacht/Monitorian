using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Forms;

using static ScreenFrame.WindowHelper;

namespace ScreenFrame;

internal static class NotifyIconHelper
{
	#region Win32

	[DllImport("Shell32.dll", SetLastError = true)]
	private static extern int Shell_NotifyIconGetRect(
		[In] ref NOTIFYICONIDENTIFIER identifier,
		out RECT iconLocation);

	[StructLayout(LayoutKind.Sequential)]
	private struct NOTIFYICONIDENTIFIER
	{
		public uint cbSize;
		public IntPtr hWnd;
		public uint uID;
		public Guid guidItem;
	}

	#endregion

	/// <summary>
	/// Sets the window of a specified NotifyIcon into foreground.
	/// </summary>
	/// <param name="notifyIcon">NotifyIcon</param>
	/// <returns>True if successfully sets</returns>
	public static bool SetNotifyIconWindowForeground(NotifyIcon notifyIcon)
	{
		if (!TryGetNotifyIconWindow(notifyIcon, out NativeWindow window))
			return false;

		return SetForegroundWindow(window.Handle);
	}

	/// <summary>
	/// Attempts to get the point where a specified NotifyIcon is clicked.
	/// </summary>
	/// <param name="notifyIcon">NotifyIcon</param>
	/// <param name="point">Clicked point</param>
	/// <returns>True if successfully gets</returns>
	/// <remarks>MouseEventArgs.Location property of MouseClick event does not contain data.</remarks>
	public static bool TryGetNotifyIconClickedPoint(NotifyIcon notifyIcon, out Point point)
	{
		if (TryGetNotifyIconRect(notifyIcon, out Rect iconRect))
		{
			if (CursorHelper.TryGetCursorPoint(out POINT source))
			{
				point = source;
				if (iconRect.Contains(point))
					return true;
			}
			point = iconRect.Location; // Fallback
			return true;
		}
		point = default;
		return false;
	}

	/// <summary>
	/// Attempts to get the rectangle of a specified NotifyIcon.
	/// </summary>
	/// <param name="notifyIcon">NotifyIcon</param>
	/// <param name="iconRect">Rectangle of the NotifyIcon</param>
	/// <returns>True if successfully gets</returns>
	/// <remarks>
	/// The idea to get the rectangle of a NotifyIcon is derived from:
	/// https://github.com/rzhw/SuperNotifyIcon
	/// </remarks>
	public static bool TryGetNotifyIconRect(NotifyIcon notifyIcon, out Rect iconRect)
	{
		iconRect = Rect.Empty;

		if (!TryGetNotifyIconIdentifier(notifyIcon, out NOTIFYICONIDENTIFIER identifier))
			return false;

		var result = Shell_NotifyIconGetRect(ref identifier, out RECT iconLocation);
		switch (result)
		{
			case S_OK:
			case S_FALSE:
				iconRect = iconLocation;
				return true;
			default:
				return false;
		}
	}

	private static bool TryGetNotifyIconIdentifier(NotifyIcon notifyIcon, out NOTIFYICONIDENTIFIER identifier)
	{
		identifier = new NOTIFYICONIDENTIFIER { cbSize = (uint)Marshal.SizeOf<NOTIFYICONIDENTIFIER>() };

		if (!TryGetNonPublicFieldValue(notifyIcon, "id", out int id))
			return false;

		if (!TryGetNotifyIconWindow(notifyIcon, out NativeWindow window))
			return false;

		identifier.uID = (uint)id;
		identifier.hWnd = window.Handle;
		return true;
	}

	public static bool TryGetNotifyIconWindow(NotifyIcon notifyIcon, out NativeWindow window) =>
		TryGetNonPublicFieldValue(notifyIcon, "window", out window);

	private static bool TryGetNonPublicFieldValue<TInstance, TValue>(TInstance instance, string fieldName, out TValue fieldValue)
	{
		var fieldInfo = typeof(TInstance).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
		if (fieldInfo?.GetValue(instance) is TValue value)
		{
			fieldValue = value;
			return true;
		}
		fieldValue = default;
		return false;
	}
}