using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monitorian.Core.Helper
{
	public static class EnumerableExtension
	{
		public static bool TryGetKey<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> pairs, TValue value, out TKey key)
		{
			if (pairs is null)
				throw new ArgumentNullException(nameof(pairs));

			foreach (var (k, v) in pairs)
			{
				if (EqualityComparer<TValue>.Default.Equals(v, value))
				{
					key = k;
					return true;
				}
			}
			key = default;
			return false;
		}

		public static TKey GetKeyOrDefault<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> pairs, TValue value)
		{
			TryGetKey(pairs, value, out TKey key);
			return key;
		}

		public static IEnumerable<TSource[]> Split<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> separator)
		{
			if (source is null)
				throw new ArgumentNullException(nameof(source));
			if (separator is null)
				throw new ArgumentNullException(nameof(separator));

			var buffer = new List<TSource>();

			foreach (var item in source)
			{
				if (separator.Invoke(item) && buffer.Any())
				{
					yield return buffer.ToArray();
					buffer.Clear();
				}
				buffer.Add(item);
			}

			if (buffer.Any())
			{
				yield return buffer.ToArray();
			}
		}
	}
}