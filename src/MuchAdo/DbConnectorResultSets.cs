namespace MuchAdo;

/// <summary>
/// Encapsulates multiple result sets.
/// </summary>
public readonly struct DbConnectorResultSets : IDisposable, IAsyncDisposable
{
	/// <summary>
	/// Reads a result set, converting each record to the specified type.
	/// </summary>
	/// <seealso cref="ReadAsync{T}(CancellationToken)" />
	public IReadOnlyList<T> Read<T>() =>
		m_connector.ReadResultSet<T>(null);

	/// <summary>
	/// Reads a result set, converting each record to the specified type with the specified delegate.
	/// </summary>
	/// <seealso cref="ReadAsync{T}(Func{DbConnectorRecord, T}, CancellationToken)" />
	public IReadOnlyList<T> Read<T>(Func<DbConnectorRecord, T> map) =>
		m_connector.ReadResultSet(map ?? throw new ArgumentNullException(nameof(map)));

	/// <summary>
	/// Reads a result set, converting each record to the specified type.
	/// </summary>
	/// <seealso cref="Read{T}()" />
	public ValueTask<IReadOnlyList<T>> ReadAsync<T>(CancellationToken cancellationToken = default) =>
		m_connector.ReadResultSetAsync<T>(null, cancellationToken);

	/// <summary>
	/// Reads a result set, converting each record to the specified type with the specified delegate.
	/// </summary>
	/// <seealso cref="Read{T}(Func{DbConnectorRecord, T})" />
	public ValueTask<IReadOnlyList<T>> ReadAsync<T>(Func<DbConnectorRecord, T> map, CancellationToken cancellationToken = default) =>
		m_connector.ReadResultSetAsync(map ?? throw new ArgumentNullException(nameof(map)), cancellationToken);

	/// <summary>
	/// Reads a result set, reading one record at a time and converting it to the specified type.
	/// </summary>
	/// <seealso cref="EnumerateAsync{T}(CancellationToken)" />
	public IEnumerable<T> Enumerate<T>() =>
		m_connector.EnumerateResultSet<T>(null);

	/// <summary>
	/// Reads a result set, reading one record at a time and converting it to the specified type with the specified delegate.
	/// </summary>
	/// <seealso cref="EnumerateAsync{T}(Func{DbConnectorRecord, T}, CancellationToken)" />
	public IEnumerable<T> Enumerate<T>(Func<DbConnectorRecord, T> map) =>
		m_connector.EnumerateResultSet(map ?? throw new ArgumentNullException(nameof(map)));

	/// <summary>
	/// Reads a result set, reading one record at a time and converting it to the specified type.
	/// </summary>
	/// <seealso cref="Enumerate{T}()" />
	public IAsyncEnumerable<T> EnumerateAsync<T>(CancellationToken cancellationToken = default) =>
		m_connector.EnumerateResultSetAsync<T>(null, cancellationToken);

	/// <summary>
	/// Reads a result set, reading one record at a time and converting it to the specified type with the specified delegate.
	/// </summary>
	/// <seealso cref="Enumerate{T}(Func{DbConnectorRecord, T})" />
	public IAsyncEnumerable<T> EnumerateAsync<T>(Func<DbConnectorRecord, T> map, CancellationToken cancellationToken = default) =>
		m_connector.EnumerateResultSetAsync(map ?? throw new ArgumentNullException(nameof(map)), cancellationToken);

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

	private readonly DbConnector m_connector;
}
