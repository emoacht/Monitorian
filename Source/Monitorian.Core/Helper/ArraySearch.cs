using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monitorian.Core.Helper
{
	/// <summary>
	/// Search methods for <see cref="System.Array"/>
	/// </summary>
	/// <remarks>The array must be sorted beforehand.</remarks>
	public static class ArraySearch
	{
		public static int GetNearest(int[] array, int target) =>
			array[GetNearestIndex(array, target)];

		public static int GetNearestIndex(int[] array, int target) =>
			GetNearestIndex(array, target, (a, b) => Math.Abs(a - b));

		public static byte GetNearest(byte[] array, byte target) =>
			array[GetNearestIndex(array, target)];

		public static int GetNearestIndex(byte[] array, byte target) =>
			GetNearestIndex(array, target, (a, b) => (byte)Math.Abs(a - b));

		public static int GetNearestIndex<T>(T[] array, T target, Func<T, T, T> measure) where T : IComparable
		{
			if (array is not { Length: > 0 })
				throw new ArgumentNullException(nameof(array));

			// The source array must be sorted beforehand.
			int indexExact = Array.BinarySearch(array, target);

			if (indexExact >= 0)
				return indexExact;

			int indexAfter = ~indexExact; // Index of first element that is larger than target

			if (indexAfter == 0)
				return 0; // First index

			if (indexAfter == array.Length)
				return array.Length - 1; // Last index

			T gapBefore = measure(target, array[indexAfter - 1]);
			T gapAfter = measure(array[indexAfter], target);

			if (gapBefore.CompareTo(gapAfter) <= 0)
				return indexAfter - 1;
			else
				return indexAfter;
		}
	}
}