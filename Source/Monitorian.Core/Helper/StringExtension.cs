using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Monitorian.Core.Helper;

public static class StringExtension
{
	/// <summary>
	/// Determines whether a specified string is composed of all ASCII characters.
	/// </summary>
	/// <param name="source">Source string</param>
	/// <returns>True if all ASCII characters</returns>
	public static bool IsAscii(this string source)
	{
		return source?.All(x => x <= 0x7F) is true;
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
		if (source is null)
			throw new ArgumentNullException(nameof(source));

		var buffer = new StringBuilder();
		var isFound = false;

		foreach (char c in source)
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

	public static int[] IndicesOf(this string source, string value, StringComparison comparisonType) =>
		IndicesOf(source, value, int.MaxValue, comparisonType);

	public static int[] IndicesOf(this string source, string value, int count, StringComparison comparisonType)
	{
		if (source is null)
			throw new ArgumentNullException(nameof(source));
		if (string.IsNullOrEmpty(value))
			throw new ArgumentNullException(nameof(value));
		if (count <= 0)
			throw new ArgumentOutOfRangeException(nameof(count), count, "The count must be greater than 0.");

		var indices = new List<int>();
		int startIndex = 0;
		int lastIndex = source.Length - value.Length;

		while (startIndex <= lastIndex)
		{
			var foundIndex = source.IndexOf(value, startIndex, comparisonType);
			if (foundIndex < 0)
				break;

			indices.Add(foundIndex);
			if (indices.Count >= count)
				break;

			startIndex = foundIndex + value.Length;
		}
		return indices.ToArray();
	}
}