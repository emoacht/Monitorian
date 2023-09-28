using System.Collections.Generic;

namespace Monitorian.Core.Helper;

public static class KeyValuePairExtension
{
	public static void Deconstruct<TKey, TValue>(this KeyValuePair<TKey, TValue> pair, out TKey key, out TValue value)
	{
		key = pair.Key;
		value = pair.Value;
	}
}