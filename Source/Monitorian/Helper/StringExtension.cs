using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monitorian.Helper
{
	/// <summary>
	/// Extension methods for <see cref="String"/>
	/// </summary>
	public static class StringExtension
	{
		public static bool IsAscii(this string source)
		{
			if (source == null)
				return true;

			return source.Select(x => (int)x).All(x => x < 0x80);
		}
	}
}