#if NETSTANDARD2_0
using System.Diagnostics.CodeAnalysis;
#endif

namespace MuchAdo;

internal static class Utility
{
	public static IEnumerable<T> Memoize<T>(this IEnumerable<T> items) =>
		items is IReadOnlyCollection<T> or ICollection<T> ? items : [.. items];

	public static bool ContainsOrdinal(this string str, char value)
	{
#if !NETSTANDARD2_0
		return str.Contains(value, StringComparison.Ordinal);
#else
		return str.Contains(value);
#endif
	}

	public static bool ContainsOrdinal(this string str, string value)
	{
#if !NETSTANDARD2_0
		return str.Contains(value, StringComparison.Ordinal);
#else
		return str.Contains(value);
#endif
	}

	public static string ReplaceOrdinal(this string str, string oldValue, string newValue)
	{
#if !NETSTANDARD2_0
		return str.Replace(oldValue, newValue, StringComparison.Ordinal);
#else
		return str.Replace(oldValue, newValue);
#endif
	}

	public static int CombineHashCodes(int value1, int value2)
	{
#if !NETSTANDARD2_0
		return HashCode.Combine(value1, value2);
#else
		return value1 * 397 ^ value2;
#endif
	}

#if NETSTANDARD2_0
	public static TValue? GetValueOrDefault<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> dictionary, TKey key) =>
		dictionary.TryGetValue(key, out var obj) ? obj : default;

	public static bool Remove<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, [MaybeNullWhen(false)] out TValue value)
	{
		if (!dictionary.TryGetValue(key, out value))
			return false;

		dictionary.Remove(key);
		return true;
	}
#endif
}
