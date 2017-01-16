using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monitorian.Helper
{
	/// <summary>
	/// Array search methods
	/// </summary>
	public static class ArraySearch
	{
		public static int GetNearest(int[] source, int target) =>
			source[GetNearestIndex(source, target)];

		public static int GetNearestIndex(int[] source, int target) =>
			GetNearestIndex(source, target, (a, b) => Math.Abs(a - b));

		public static int GetNearest(byte[] source, byte target) =>
			source[GetNearestIndex(source, target)];

		public static int GetNearestIndex(byte[] source, byte target) =>
			GetNearestIndex(source, target, (a, b) => (byte)Math.Abs(a - b));

		public static int GetNearestIndex<T>(T[] source, T target, Func<T, T, T> measure) where T : IComparable
		{
			if ((source == null) || !source.Any())
				throw new ArgumentNullException(nameof(source));

			// The source array must be sorted.
			int indexExact = Array.BinarySearch(source, target);

			if (indexExact >= 0)
				return indexExact;

			int indexAfter = ~indexExact; // Index of first element that is larger than target

			if (indexAfter == 0)
				return 0; // First index

			if (indexAfter == source.Length)
				return source.Length - 1; // Last index

			T gapBefore = measure(target, source[indexAfter - 1]);
			T gapAfter = measure(source[indexAfter], target);

			if (gapBefore.CompareTo(gapAfter) <= 0)
				return indexAfter - 1;
			else
				return indexAfter;
		}
	}
}