using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monitorian.Core.Helper
{
	/// <summary>
	/// Extension methods for <see cref="String"/>
	/// </summary>
	public static class StringExtension
	{
		/// <summary>
		/// Indicates whether a specified string is composed of all ASCII characters.
		/// </summary>
		/// <param name="source">Source string</param>
		/// <returns>True if all ASCII characters</returns>
		public static bool IsAscii(this string source)
		{
			if (source is null)
				return true;

			return source.Select(x => (int)x).All(x => x < 0x80);
		}

		/// <summary>
		/// Replaces a specified character with the characters of a specified length. 
		/// </summary>
		/// <param name="source">Source string</param>
		/// <param name="value">Target character</param>
		/// <param name="repeatCount">Length of characters</param>
		/// <returns>Replaced string</returns>
		public static string Replace(this string source, char value, int repeatCount)
		{
			var buffer = new StringBuilder();
			var isFound = false;

			foreach (var c in source)
			{
				if (char.Equals(value, c))
				{
					isFound = true;
					continue;
				}

				if (isFound)
				{
					isFound = false;
					buffer.Append(value, repeatCount);
				}
				buffer.Append(c);
			}

			if (isFound)
			{
				buffer.Append(value, repeatCount);
			}

			return buffer.ToString();
		}
	}
}