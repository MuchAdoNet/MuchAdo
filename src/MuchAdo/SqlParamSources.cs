using System.Collections;

namespace MuchAdo;

/// <summary>
/// A list of parameter sources.
/// </summary>
public sealed class SqlParamSources : SqlParamSource, IList<SqlParamSource>, IReadOnlyList<SqlParamSource>
{
	/// <summary>
	/// Creates an empty list.
	/// </summary>
	public SqlParamSources() => m_sources = [];

	/// <summary>
	/// Creates a list from the specified parameter sources.
	/// </summary>
	public SqlParamSources(params ReadOnlySpan<SqlParamSource> items) => m_sources = [.. items];

	/// <summary>
	/// Creates a list from the specified parameter sources.
	/// </summary>
	public SqlParamSources(IEnumerable<SqlParamSource> items) => m_sources = [.. items];

	/// <inheritdoc />
	public void Add(SqlParamSource source) => m_sources.Add(source);

	/// <inheritdoc />
	public void Clear() => m_sources.Clear();

	/// <inheritdoc />
	public bool Contains(SqlParamSource item) => m_sources.Contains(item);

	/// <inheritdoc />
	public void CopyTo(SqlParamSource[] array, int arrayIndex) => m_sources.CopyTo(array, arrayIndex);

	/// <inheritdoc />
	public bool Remove(SqlParamSource item) => m_sources.Remove(item);

	public int Count => m_sources.Count;

	/// <inheritdoc />
	public IEnumerator<SqlParamSource> GetEnumerator() => m_sources.GetEnumerator();

	/// <inheritdoc />
	IEnumerator IEnumerable.GetEnumerator() => m_sources.GetEnumerator();

	/// <inheritdoc />
	public int IndexOf(SqlParamSource item) => m_sources.IndexOf(item);

	/// <inheritdoc />
	public void Insert(int index, SqlParamSource item) => m_sources.Insert(index, item);

	/// <inheritdoc />
	public void RemoveAt(int index) => m_sources.RemoveAt(index);

	public SqlParamSource this[int index]
	{
		get => m_sources[index];
		set => m_sources[index] = value;
	}

	/// <inheritdoc />
	bool ICollection<SqlParamSource>.IsReadOnly => false;

	internal override void SubmitParameters(ISqlParamTarget target)
	{
		foreach (var source in m_sources)
			source.SubmitParameters(target);
	}

	private readonly List<SqlParamSource> m_sources;
}
