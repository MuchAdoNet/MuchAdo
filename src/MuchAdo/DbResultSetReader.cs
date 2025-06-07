namespace MuchAdo;

/// <summary>
/// Provides access to multiple result sets.
/// </summary>
public readonly struct DbResultSetReader : IDisposable, IAsyncDisposable
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
	/// Reads the first record from a result set, converting it to the specified type.
	/// </summary>
	/// <exception cref="InvalidOperationException">Thrown if no records are found.</exception>
	/// <seealso cref="ReadFirstAsync{T}(CancellationToken)" />
	/// <seealso cref="ReadFirstOrDefault{T}()" />
	public T ReadFirst<T>() =>
		m_connector.ReadResultSetFirst<T>(null, single: false, orDefault: false);

	/// <summary>
	/// Reads the first record from a result set, converting it to the specified type with the specified delegate.
	/// </summary>
	/// <exception cref="InvalidOperationException">Thrown if no records are found.</exception>
	/// <seealso cref="ReadFirstAsync{T}(Func{DbConnectorRecord, T}, CancellationToken)" />
	/// <seealso cref="ReadFirstOrDefault{T}(Func{DbConnectorRecord, T})" />
	public T ReadFirst<T>(Func<DbConnectorRecord, T> map) =>
		m_connector.ReadResultSetFirst(map ?? throw new ArgumentNullException(nameof(map)), single: false, orDefault: false);

	/// <summary>
	/// Reads the first record from a result set, converting it to the specified type, or returns the default value if no records are found.
	/// </summary>
	/// <seealso cref="ReadFirstOrDefaultAsync{T}(CancellationToken)" />
	/// <seealso cref="ReadFirst{T}()" />
	public T ReadFirstOrDefault<T>() =>
		m_connector.ReadResultSetFirst<T>(null, single: false, orDefault: true);

	/// <summary>
	/// Reads the first record from a result set, converting it to the specified type with the specified delegate, or returns the default value if no records are found.
	/// </summary>
	/// <seealso cref="ReadFirstOrDefaultAsync{T}(Func{DbConnectorRecord, T}, CancellationToken)" />
	/// <seealso cref="ReadFirst{T}(Func{DbConnectorRecord, T})" />
	public T ReadFirstOrDefault<T>(Func<DbConnectorRecord, T> map) =>
		m_connector.ReadResultSetFirst(map ?? throw new ArgumentNullException(nameof(map)), single: false, orDefault: true);

	/// <summary>
	/// Reads the single record from a result set, converting it to the specified type.
	/// </summary>
	/// <exception cref="InvalidOperationException">Thrown if no records are found or if more than one record is found.</exception>
	/// <seealso cref="ReadSingleAsync{T}(CancellationToken)" />
	/// <seealso cref="ReadSingleOrDefault{T}()" />
	public T ReadSingle<T>() =>
		m_connector.ReadResultSetFirst<T>(null, single: true, orDefault: false);

	/// <summary>
	/// Reads the single record from a result set, converting it to the specified type with the specified delegate.
	/// </summary>
	/// <exception cref="InvalidOperationException">Thrown if no records are found or if more than one record is found.</exception>
	/// <seealso cref="ReadSingleAsync{T}(Func{DbConnectorRecord, T}, CancellationToken)" />
	/// <seealso cref="ReadSingleOrDefault{T}(Func{DbConnectorRecord, T})" />
	public T ReadSingle<T>(Func<DbConnectorRecord, T> map) =>
		m_connector.ReadResultSetFirst(map ?? throw new ArgumentNullException(nameof(map)), single: true, orDefault: false);

	/// <summary>
	/// Reads the single record from a result set, converting it to the specified type, or returns the default value if no records are found.
	/// </summary>
	/// <exception cref="InvalidOperationException">Thrown if more than one record is found.</exception>
	/// <seealso cref="ReadSingleOrDefaultAsync{T}(CancellationToken)" />
	/// <seealso cref="ReadSingle{T}()" />
	public T ReadSingleOrDefault<T>() =>
		m_connector.ReadResultSetFirst<T>(null, single: true, orDefault: true);

	/// <summary>
	/// Reads the single record from a result set, converting it to the specified type with the specified delegate, or returns the default value if no records are found.
	/// </summary>
	/// <exception cref="InvalidOperationException">Thrown if more than one record is found.</exception>
	/// <seealso cref="ReadSingleOrDefaultAsync{T}(Func{DbConnectorRecord, T}, CancellationToken)" />
	/// <seealso cref="ReadSingle{T}(Func{DbConnectorRecord, T})" />
	public T ReadSingleOrDefault<T>(Func<DbConnectorRecord, T> map) =>
		m_connector.ReadResultSetFirst(map ?? throw new ArgumentNullException(nameof(map)), single: true, orDefault: true);

	/// <summary>
	/// Reads the first record from a result set, converting it to the specified type.
	/// </summary>
	/// <exception cref="InvalidOperationException">Thrown if no records are found.</exception>
	/// <seealso cref="ReadFirst{T}()" />
	/// <seealso cref="ReadFirstOrDefaultAsync{T}(CancellationToken)" />
	public ValueTask<T> ReadFirstAsync<T>(CancellationToken cancellationToken = default) =>
		m_connector.ReadResultSetFirstAsync<T>(null, single: false, orDefault: false, cancellationToken);

	/// <summary>
	/// Reads the first record from a result set, converting it to the specified type with the specified delegate.
	/// </summary>
	/// <exception cref="InvalidOperationException">Thrown if no records are found.</exception>
	/// <seealso cref="ReadFirst{T}(Func{DbConnectorRecord, T})" />
	/// <seealso cref="ReadFirstOrDefaultAsync{T}(Func{DbConnectorRecord, T}, CancellationToken)" />
	public ValueTask<T> ReadFirstAsync<T>(Func<DbConnectorRecord, T> map, CancellationToken cancellationToken = default) =>
		m_connector.ReadResultSetFirstAsync(map ?? throw new ArgumentNullException(nameof(map)), single: false, orDefault: false, cancellationToken);

	/// <summary>
	/// Reads the first record from a result set, converting it to the specified type, or returns the default value if no records are found.
	/// </summary>
	/// <seealso cref="ReadFirstOrDefault{T}()" />
	/// <seealso cref="ReadFirstAsync{T}(CancellationToken)" />
	public async ValueTask<T?> ReadFirstOrDefaultAsync<T>(CancellationToken cancellationToken = default) =>
		await m_connector.ReadResultSetFirstAsync<T>(null, single: false, orDefault: true, cancellationToken).ConfigureAwait(false);

	/// <summary>
	/// Reads the first record from a result set, converting it to the specified type with the specified delegate, or returns the default value if no records are found.
	/// </summary>
	/// <seealso cref="ReadFirstOrDefault{T}(Func{DbConnectorRecord, T})" />
	/// <seealso cref="ReadFirstAsync{T}(Func{DbConnectorRecord, T}, CancellationToken)" />
	public async ValueTask<T?> ReadFirstOrDefaultAsync<T>(Func<DbConnectorRecord, T> map, CancellationToken cancellationToken = default) =>
		await m_connector.ReadResultSetFirstAsync(map ?? throw new ArgumentNullException(nameof(map)), single: false, orDefault: true, cancellationToken).ConfigureAwait(false);

	/// <summary>
	/// Reads the single record from a result set, converting it to the specified type.
	/// </summary>
	/// <exception cref="InvalidOperationException">Thrown if no records are found or if more than one record is found.</exception>
	/// <seealso cref="ReadSingle{T}()" />
	/// <seealso cref="ReadSingleOrDefaultAsync{T}(CancellationToken)" />
	public ValueTask<T> ReadSingleAsync<T>(CancellationToken cancellationToken = default) =>
		m_connector.ReadResultSetFirstAsync<T>(null, single: true, orDefault: false, cancellationToken);

	/// <summary>
	/// Reads the single record from a result set, converting it to the specified type with the specified delegate.
	/// </summary>
	/// <exception cref="InvalidOperationException">Thrown if no records are found or if more than one record is found.</exception>
	/// <seealso cref="ReadSingle{T}(Func{DbConnectorRecord, T})" />
	/// <seealso cref="ReadSingleOrDefaultAsync{T}(Func{DbConnectorRecord, T}, CancellationToken)" />
	public ValueTask<T> ReadSingleAsync<T>(Func<DbConnectorRecord, T> map, CancellationToken cancellationToken = default) =>
		m_connector.ReadResultSetFirstAsync(map ?? throw new ArgumentNullException(nameof(map)), single: true, orDefault: false, cancellationToken);

	/// <summary>
	/// Reads the single record from a result set, converting it to the specified type, or returns the default value if no records are found.
	/// </summary>
	/// <exception cref="InvalidOperationException">Thrown if more than one record is found.</exception>
	/// <seealso cref="ReadSingleOrDefault{T}()" />
	/// <seealso cref="ReadSingleAsync{T}(CancellationToken)" />
	public async ValueTask<T?> ReadSingleOrDefaultAsync<T>(CancellationToken cancellationToken = default) =>
		await m_connector.ReadResultSetFirstAsync<T>(null, single: true, orDefault: true, cancellationToken).ConfigureAwait(false);

	/// <summary>
	/// Reads the single record from a result set, converting it to the specified type with the specified delegate, or returns the default value if no records are found.
	/// </summary>
	/// <exception cref="InvalidOperationException">Thrown if more than one record is found.</exception>
	/// <seealso cref="ReadSingleOrDefault{T}(Func{DbConnectorRecord, T})" />
	/// <seealso cref="ReadSingleAsync{T}(Func{DbConnectorRecord, T}, CancellationToken)" />
	public async ValueTask<T?> ReadSingleOrDefaultAsync<T>(Func<DbConnectorRecord, T> map, CancellationToken cancellationToken = default) =>
		await m_connector.ReadResultSetFirstAsync(map ?? throw new ArgumentNullException(nameof(map)), single: true, orDefault: true, cancellationToken).ConfigureAwait(false);

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
		m_connector.DisposeActiveCommandOrBatch();
	}

	/// <summary>
	/// Disposes resources used by the result sets.
	/// </summary>
	/// <seealso cref="Dispose" />
	public async ValueTask DisposeAsync()
	{
		await m_connector.DisposeActiveReaderAsync().ConfigureAwait(false);
		await m_connector.DisposeActiveCommandOrBatchAsync().ConfigureAwait(false);
	}

	internal DbResultSetReader(DbConnector connector)
	{
		m_connector = connector;
	}

	private readonly DbConnector m_connector;
}
