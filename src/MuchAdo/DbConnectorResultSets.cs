using System.Runtime.CompilerServices;

namespace MuchAdo;

/// <summary>
/// Encapsulates multiple result sets.
/// </summary>
public sealed class DbConnectorResultSets : IDisposable, IAsyncDisposable
{
	/// <summary>
	/// Reads a result set, converting each record to the specified type.
	/// </summary>
	/// <seealso cref="ReadAsync{T}(CancellationToken)" />
	public IReadOnlyList<T> Read<T>() =>
		DoRead<T>(null);

	/// <summary>
	/// Reads a result set, converting each record to the specified type with the specified delegate.
	/// </summary>
	/// <seealso cref="ReadAsync{T}(Func{DbConnectorRecord, T}, CancellationToken)" />
	public IReadOnlyList<T> Read<T>(Func<DbConnectorRecord, T> map) =>
		DoRead(map ?? throw new ArgumentNullException(nameof(map)));

	/// <summary>
	/// Reads a result set, converting each record to the specified type.
	/// </summary>
	/// <seealso cref="Read{T}()" />
	public ValueTask<IReadOnlyList<T>> ReadAsync<T>(CancellationToken cancellationToken = default) =>
		DoReadAsync<T>(null, cancellationToken);

	/// <summary>
	/// Reads a result set, converting each record to the specified type with the specified delegate.
	/// </summary>
	/// <seealso cref="Read{T}(Func{DbConnectorRecord, T})" />
	public ValueTask<IReadOnlyList<T>> ReadAsync<T>(Func<DbConnectorRecord, T> map, CancellationToken cancellationToken = default) =>
		DoReadAsync(map ?? throw new ArgumentNullException(nameof(map)), cancellationToken);

	/// <summary>
	/// Reads a result set, reading one record at a time and converting it to the specified type.
	/// </summary>
	/// <seealso cref="EnumerateAsync{T}(CancellationToken)" />
	public IEnumerable<T> Enumerate<T>() =>
		DoEnumerate<T>(null);

	/// <summary>
	/// Reads a result set, reading one record at a time and converting it to the specified type with the specified delegate.
	/// </summary>
	/// <seealso cref="EnumerateAsync{T}(Func{DbConnectorRecord, T}, CancellationToken)" />
	public IEnumerable<T> Enumerate<T>(Func<DbConnectorRecord, T> map) =>
		DoEnumerate(map ?? throw new ArgumentNullException(nameof(map)));

	/// <summary>
	/// Reads a result set, reading one record at a time and converting it to the specified type.
	/// </summary>
	/// <seealso cref="Enumerate{T}()" />
	public IAsyncEnumerable<T> EnumerateAsync<T>(CancellationToken cancellationToken = default) =>
		DoEnumerateAsync<T>(null, cancellationToken);

	/// <summary>
	/// Reads a result set, reading one record at a time and converting it to the specified type with the specified delegate.
	/// </summary>
	/// <seealso cref="Enumerate{T}(Func{DbConnectorRecord, T})" />
	public IAsyncEnumerable<T> EnumerateAsync<T>(Func<DbConnectorRecord, T> map, CancellationToken cancellationToken = default) =>
		DoEnumerateAsync(map ?? throw new ArgumentNullException(nameof(map)), cancellationToken);

	/// <summary>
	/// Disposes resources used by the result sets.
	/// </summary>
	/// <seealso cref="DisposeAsync" />
	public void Dispose()
	{
		m_connector.DisposeActiveReader();
		m_connector.DisposeActiveCommand();
	}

	/// <summary>
	/// Disposes resources used by the result sets.
	/// </summary>
	/// <seealso cref="Dispose" />
	public async ValueTask DisposeAsync()
	{
		await m_connector.DisposeActiveReaderAsync().ConfigureAwait(false);
		await m_connector.DisposeActiveCommandAsync().ConfigureAwait(false);
	}

	internal DbConnectorResultSets(DbConnector connector)
	{
		m_connector = connector;
	}

	private IReadOnlyList<T> DoRead<T>(Func<DbConnectorRecord, T>? map)
	{
		if (m_next && !m_connector.NextReaderResultCore())
			throw CreateNoMoreResultsException();
		m_next = true;

		var list = new List<T>();
		var record = new DbConnectorRecord(m_connector, new DbConnectorRecordState());
		while (m_connector.ReadReaderCore())
			list.Add(map is not null ? map(record) : record.Get<T>());
		return list;
	}

	private async ValueTask<IReadOnlyList<T>> DoReadAsync<T>(Func<DbConnectorRecord, T>? map, CancellationToken cancellationToken)
	{
		if (m_next && !await m_connector.NextReaderResultCoreAsync(cancellationToken).ConfigureAwait(false))
			throw CreateNoMoreResultsException();
		m_next = true;

		var list = new List<T>();
		var record = new DbConnectorRecord(m_connector, new DbConnectorRecordState());
		while (await m_connector.ReadReaderCoreAsync(cancellationToken).ConfigureAwait(false))
			list.Add(map is not null ? map(record) : record.Get<T>());
		return list;
	}

	private IEnumerable<T> DoEnumerate<T>(Func<DbConnectorRecord, T>? map)
	{
		if (m_next && !m_connector.NextReaderResultCore())
			throw CreateNoMoreResultsException();
		m_next = true;

		var record = new DbConnectorRecord(m_connector, new DbConnectorRecordState());
		while (m_connector.ReadReaderCore())
			yield return map is not null ? map(record) : record.Get<T>();
	}

	private async IAsyncEnumerable<T> DoEnumerateAsync<T>(Func<DbConnectorRecord, T>? map, [EnumeratorCancellation] CancellationToken cancellationToken)
	{
		if (m_next && !await m_connector.NextReaderResultCoreAsync(cancellationToken).ConfigureAwait(false))
			throw CreateNoMoreResultsException();
		m_next = true;

		var record = new DbConnectorRecord(m_connector, new DbConnectorRecordState());
		while (await m_connector.ReadReaderCoreAsync(cancellationToken).ConfigureAwait(false))
			yield return map is not null ? map(record) : record.Get<T>();
	}

	private static InvalidOperationException CreateNoMoreResultsException() => new("No more results.");

	private readonly DbConnector m_connector;
	private bool m_next;
}
