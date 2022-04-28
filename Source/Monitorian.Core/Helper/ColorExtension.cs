using System.Windows.Media;

namespace Monitorian.Core.Helper;

public static class ColorExtension
{
	public static Color ToWindowsMediaColor(this Windows.UI.Color color)
		=> new() {
			A = color.A,
			R = color.R,
			G = color.G,
			B = color.B,
		};
}