namespace MuchAdo;

internal sealed class DbCommandCache
{
	public object? TryRemoveCommand(object key) => m_dictionary.Remove(key, out var command) ? command : null;

	public void AddCommand(object key, object command) => m_dictionary.Add(key, command);

	public IReadOnlyCollection<object> GetCommandCollection() => m_dictionary.Values;

	private readonly Dictionary<object, object> m_dictionary = new(KeyComparer.Instance);

	private sealed class KeyComparer : IEqualityComparer<object>
	{
		public static readonly IEqualityComparer<object> Instance = new KeyComparer();

		bool IEqualityComparer<object>.Equals(object? x, object? y)
		{
			if (x is IEnumerable<object> xs && y is IEnumerable<object> ys)
				return xs.SequenceEqual(ys, Instance);

			return Equals(x, y);
		}

		int IEqualityComparer<object>.GetHashCode(object obj)
		{
			if (obj is IEnumerable<object> items)
			{
				var hash = 0;
				foreach (var item in items)
					hash = Utility.CombineHashCodes(hash, Instance.GetHashCode(item));
				return hash;
			}

			return obj.GetHashCode();
		}
	}
}
