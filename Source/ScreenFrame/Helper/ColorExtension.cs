using System;
using System.Runtime.InteropServices;
using System.Windows.Media;

namespace ScreenFrame.Helper;

internal static class ColorExtension
{
	#region Win32

	[DllImport("Dwmapi.dll")]
	private static extern int DwmGetColorizationColor(
		out uint pcrColorization,
		[MarshalAs(UnmanagedType.Bool)] out bool pfOpaqueBlend);

	private const int S_OK = 0x0;

	#endregion

	public static Color GetColorizationColor()
	{
		if (DwmGetColorizationColor(
			out uint pcrColorization,
			out _) == S_OK)
		{
			return FromUInt32(pcrColorization);
		}
		return default;
	}

	public static uint ToUInt32(this Color color)
	{
		var bytes = new[] { color.B, color.G, color.R, color.A };
		return BitConverter.ToUInt32(bytes, 0);
	}

	public static Color FromUInt32(uint value)
	{
		var bytes = BitConverter.GetBytes(value);
		return Color.FromArgb(bytes[3], bytes[2], bytes[1], bytes[0]);
	}
}