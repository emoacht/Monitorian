using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace Monitorian.Core.Helper
{
	internal static class ColorExtension
	{
		public static uint ToUInt32(this Color color)
		{
			var bytes = new[] { color.B, color.G, color.R, color.A }.ToArray();
			return BitConverter.ToUInt32(bytes, 0);
		}

		public static Color FromUInt32(this Color color, uint source)
		{
			var bytes = BitConverter.GetBytes(source);
			return Color.FromArgb(bytes[3], bytes[2], bytes[1], bytes[0]);
		}
	}
}