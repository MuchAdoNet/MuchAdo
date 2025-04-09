using System.Data;

namespace MuchAdo;

/// <summary>
/// Encapsulates the text and parameters of a database command.
/// </summary>
public sealed class DbConnectorCommand
{
	/// <summary>
	/// The text of the command.
	/// </summary>
	public string Text { get; }

	/// <summary>
	/// The parameters of the command.
	/// </summary>
	public DbParameters Parameters => m_parameters;

	/// <summary>
	/// The <see cref="CommandType"/> of the command.
	/// </summary>
	public CommandType CommandType { get; }

	/// <summary>
	/// The timeout of the command.
	/// </summary>
	/// <remarks>If not specified, the default timeout for the connection is used.</remarks>
	public TimeSpan? Timeout { get; private set; }

	/// <summary>
	/// True after <see cref="Cache"/> is called.
	/// </summary>
	public bool IsCached { get; private set; }

	/// <summary>
	/// True after <see cref="Prepare"/> is called.
	/// </summary>
	public bool IsPrepared { get; private set; }

	/// <summary>
	/// The connector of the command.
	/// </summary>
	public DbConnector Connector { get; }

	/// <summary>
	/// Executes the command, returning the number of rows affected.
	/// </summary>
	/// <seealso cref="ExecuteAsync" />
	public int Execute() =>
		Connector.ExecuteCommand(this);

	/// <summary>
	/// Executes the command, returning the number of rows affected.
	/// </summary>
	/// <seealso cref="Execute" />
	public ValueTask<int> ExecuteAsync(CancellationToken cancellationToken = default) =>
		Connector.ExecuteCommandAsync(this, cancellationToken);

	/// <summary>
	/// Executes the query, reading every record and converting it to the specified type.
	/// </summary>
	/// <seealso cref="QueryAsync{T}(CancellationToken)" />
	public IReadOnlyList<T> Query<T>() =>
		Connector.Query<T>(this, map: null);

	/// <summary>
	/// Executes the query, reading every record and converting it to the specified type with the specified delegate.
	/// </summary>
	/// <seealso cref="QueryAsync{T}(Func{DbConnectorRecord, T}, CancellationToken)" />
	public IReadOnlyList<T> Query<T>(Func<DbConnectorRecord, T> map) =>
		Connector.Query(this, map ?? throw new ArgumentNullException(nameof(map)));

	/// <summary>
	/// Executes the query, converting the first record to the specified type.
	/// </summary>
	/// <remarks>Throws <see cref="InvalidOperationException"/> if no records are returned.</remarks>
	/// <seealso cref="QueryFirstAsync{T}(CancellationToken)" />
	public T QueryFirst<T>() =>
		Connector.QueryFirst<T>(this, map: null, single: false, orDefault: false);

	/// <summary>
	/// Executes the query, converting the first record to the specified type with the specified delegate.
	/// </summary>
	/// <remarks>Throws <see cref="InvalidOperationException"/> if no records are returned.</remarks>
	/// <seealso cref="QueryFirstAsync{T}(Func{DbConnectorRecord, T}, CancellationToken)" />
	public T QueryFirst<T>(Func<DbConnectorRecord, T> map) =>
		Connector.QueryFirst(this, map ?? throw new ArgumentNullException(nameof(map)), single: false, orDefault: false);

	/// <summary>
	/// Executes the query, converting the first record to the specified type.
	/// </summary>
	/// <remarks>Returns <c>default(T)</c> if no records are returned.</remarks>
	/// <seealso cref="QueryFirstOrDefaultAsync{T}(CancellationToken)" />
	public T QueryFirstOrDefault<T>() =>
		Connector.QueryFirst<T>(this, map: null, single: false, orDefault: true);

	/// <summary>
	/// Executes the query, converting the first record to the specified type with the specified delegate.
	/// </summary>
	/// <remarks>Returns <c>default(T)</c> if no records are returned.</remarks>
	/// <seealso cref="QueryFirstOrDefaultAsync{T}(Func{DbConnectorRecord, T}, CancellationToken)" />
	public T QueryFirstOrDefault<T>(Func<DbConnectorRecord, T> map) =>
		Connector.QueryFirst(this, map ?? throw new ArgumentNullException(nameof(map)), single: false, orDefault: true);

	/// <summary>
	/// Executes the query, converting the first record to the specified type.
	/// </summary>
	/// <remarks>Throws <see cref="InvalidOperationException"/> if no records are returned, or if more than one record is returned.</remarks>
	/// <seealso cref="QuerySingleAsync{T}(CancellationToken)" />
	public T QuerySingle<T>() =>
		Connector.QueryFirst<T>(this, map: null, single: true, orDefault: false);

	/// <summary>
	/// Executes the query, converting the first record to the specified type with the specified delegate.
	/// </summary>
	/// <remarks>Throws <see cref="InvalidOperationException"/> if no records are returned, or if more than one record is returned.</remarks>
	/// <seealso cref="QuerySingleAsync{T}(Func{DbConnectorRecord, T}, CancellationToken)" />
	public T QuerySingle<T>(Func<DbConnectorRecord, T> map) =>
		Connector.QueryFirst(this, map ?? throw new ArgumentNullException(nameof(map)), single: true, orDefault: false);

	/// <summary>
	/// Executes the query, converting the first record to the specified type.
	/// </summary>
	/// <remarks>Returns <c>default(T)</c> if no records are returned.
	/// Throws <see cref="InvalidOperationException"/> if more than one record is returned.</remarks>
	/// <seealso cref="QuerySingleOrDefaultAsync{T}(CancellationToken)" />
	public T QuerySingleOrDefault<T>() =>
		Connector.QueryFirst<T>(this, map: null, single: true, orDefault: true);

	/// <summary>
	/// Executes the query, converting the first record to the specified type with the specified delegate.
	/// </summary>
	/// <remarks>Returns <c>default(T)</c> if no records are returned.
	/// Throws <see cref="InvalidOperationException"/> if more than one record is returned.</remarks>
	/// <seealso cref="QuerySingleOrDefaultAsync{T}(Func{DbConnectorRecord, T}, CancellationToken)" />
	public T QuerySingleOrDefault<T>(Func<DbConnectorRecord, T> map) =>
		Connector.QueryFirst(this, map ?? throw new ArgumentNullException(nameof(map)), single: true, orDefault: true);

	/// <summary>
	/// Executes the query, converting each record to the specified type.
	/// </summary>
	/// <seealso cref="Query{T}()" />
	public ValueTask<IReadOnlyList<T>> QueryAsync<T>(CancellationToken cancellationToken = default) =>
		Connector.QueryAsync<T>(this, map: null, cancellationToken);

	/// <summary>
	/// Executes the query, converting each record to the specified type with the specified delegate.
	/// </summary>
	/// <seealso cref="Query{T}(Func{DbConnectorRecord, T})" />
	public ValueTask<IReadOnlyList<T>> QueryAsync<T>(Func<DbConnectorRecord, T> map, CancellationToken cancellationToken = default) =>
		Connector.QueryAsync(this, map ?? throw new ArgumentNullException(nameof(map)), cancellationToken);

	/// <summary>
	/// Executes the query, converting the first record to the specified type.
	/// </summary>
	/// <remarks>Throws <see cref="InvalidOperationException"/> if no records are returned.</remarks>
	/// <seealso cref="QueryFirst{T}()" />
	public ValueTask<T> QueryFirstAsync<T>(CancellationToken cancellationToken = default) =>
		Connector.QueryFirstAsync<T>(this, map: null, single: false, orDefault: false, cancellationToken);

	/// <summary>
	/// Executes the query, converting the first record to the specified type with the specified delegate.
	/// </summary>
	/// <remarks>Throws <see cref="InvalidOperationException"/> if no records are returned.</remarks>
	/// <seealso cref="QueryFirst{T}(Func{DbConnectorRecord, T})" />
	public ValueTask<T> QueryFirstAsync<T>(Func<DbConnectorRecord, T> map, CancellationToken cancellationToken = default) =>
		Connector.QueryFirstAsync(this, map ?? throw new ArgumentNullException(nameof(map)), single: false, orDefault: false, cancellationToken);

	/// <summary>
	/// Executes the query, converting the first record to the specified type.
	/// </summary>
	/// <remarks>Returns <c>default(T)</c> if no records are returned.</remarks>
	/// <seealso cref="QueryFirstOrDefault{T}()" />
	public ValueTask<T> QueryFirstOrDefaultAsync<T>(CancellationToken cancellationToken = default) =>
		Connector.QueryFirstAsync<T>(this, map: null, single: false, orDefault: true, cancellationToken);

	/// <summary>
	/// Executes the query, converting the first record to the specified type with the specified delegate.
	/// </summary>
	/// <remarks>Returns <c>default(T)</c> if no records are returned.</remarks>
	/// <seealso cref="QueryFirstOrDefault{T}(Func{DbConnectorRecord, T})" />
	public ValueTask<T> QueryFirstOrDefaultAsync<T>(Func<DbConnectorRecord, T> map, CancellationToken cancellationToken = default) =>
		Connector.QueryFirstAsync(this, map ?? throw new ArgumentNullException(nameof(map)), single: false, orDefault: true, cancellationToken);

	/// <summary>
	/// Executes the query, converting the first record to the specified type.
	/// </summary>
	/// <remarks>Throws <see cref="InvalidOperationException"/> if no records are returned, or if more than one record is returned.</remarks>
	/// <seealso cref="QuerySingle{T}()" />
	public ValueTask<T> QuerySingleAsync<T>(CancellationToken cancellationToken = default) =>
		Connector.QueryFirstAsync<T>(this, map: null, single: true, orDefault: true, cancellationToken);

	/// <summary>
	/// Executes the query, converting the first record to the specified type with the specified delegate.
	/// </summary>
	/// <remarks>Throws <see cref="InvalidOperationException"/> if no records are returned, or if more than one record is returned.</remarks>
	/// <seealso cref="QuerySingle{T}(Func{DbConnectorRecord, T})" />
	public ValueTask<T> QuerySingleAsync<T>(Func<DbConnectorRecord, T> map, CancellationToken cancellationToken = default) =>
		Connector.QueryFirstAsync(this, map ?? throw new ArgumentNullException(nameof(map)), single: true, orDefault: false, cancellationToken);

	/// <summary>
	/// Executes the query, converting the first record to the specified type.
	/// </summary>
	/// <remarks>Returns <c>default(T)</c> if no records are returned.
	/// Throws <see cref="InvalidOperationException"/> if more than one record is returned.</remarks>
	/// <seealso cref="QuerySingleOrDefault{T}()" />
	public ValueTask<T> QuerySingleOrDefaultAsync<T>(CancellationToken cancellationToken = default) =>
		Connector.QueryFirstAsync<T>(this, map: null, single: true, orDefault: true, cancellationToken);

	/// <summary>
	/// Executes the query, converting the first record to the specified type with the specified delegate.
	/// </summary>
	/// <remarks>Returns <c>default(T)</c> if no records are returned.
	/// Throws <see cref="InvalidOperationException"/> if more than one record is returned.</remarks>
	/// <seealso cref="QuerySingleOrDefault{T}(Func{DbConnectorRecord, T})" />
	public ValueTask<T> QuerySingleOrDefaultAsync<T>(Func<DbConnectorRecord, T> map, CancellationToken cancellationToken = default) =>
		Connector.QueryFirstAsync(this, map ?? throw new ArgumentNullException(nameof(map)), single: true, orDefault: true, cancellationToken);

	/// <summary>
	/// Executes the query, reading one record at a time and converting it to the specified type.
	/// </summary>
	/// <seealso cref="EnumerateAsync{T}(CancellationToken)" />
	public IEnumerable<T> Enumerate<T>() =>
		Connector.Enumerate<T>(this, map: null);

	/// <summary>
	/// Executes the query, reading one record at a time and converting it to the specified type with the specified delegate.
	/// </summary>
	/// <seealso cref="EnumerateAsync{T}(Func{DbConnectorRecord, T}, CancellationToken)" />
	public IEnumerable<T> Enumerate<T>(Func<DbConnectorRecord, T> map) =>
		Connector.Enumerate(this, map ?? throw new ArgumentNullException(nameof(map)));

	/// <summary>
	/// Executes the query, reading one record at a time and converting it to the specified type.
	/// </summary>
	/// <seealso cref="Enumerate{T}()" />
	public IAsyncEnumerable<T> EnumerateAsync<T>(CancellationToken cancellationToken = default) =>
		Connector.EnumerateAsync<T>(this, map: null, cancellationToken);

	/// <summary>
	/// Executes the query, reading one record at a time and converting it to the specified type with the specified delegate.
	/// </summary>
	/// <seealso cref="Enumerate{T}(Func{DbConnectorRecord, T})" />
	public IAsyncEnumerable<T> EnumerateAsync<T>(Func<DbConnectorRecord, T> map, CancellationToken cancellationToken = default) =>
		Connector.EnumerateAsync(this, map ?? throw new ArgumentNullException(nameof(map)), cancellationToken);

	/// <summary>
	/// Executes the query, preparing to read multiple result sets.
	/// </summary>
	/// <seealso cref="QueryMultipleAsync" />
	public DbConnectorResultSets QueryMultiple() => Connector.QueryMultiple(this);

	/// <summary>
	/// Executes the query, preparing to read multiple result sets.
	/// </summary>
	/// <seealso cref="QueryMultiple" />
	public ValueTask<DbConnectorResultSets> QueryMultipleAsync(CancellationToken cancellationToken = default) => Connector.QueryMultipleAsync(this, cancellationToken);

	/// <summary>
	/// Sets the timeout of the command.
	/// </summary>
	/// <remarks>Use <see cref="System.Threading.Timeout.InfiniteTimeSpan" /> (not <see cref="TimeSpan.Zero" />) for infinite timeout.</remarks>
	/// <exception cref="ArgumentOutOfRangeException"><c>timeSpan</c> is not positive or <see cref="System.Threading.Timeout.InfiniteTimeSpan" />.</exception>
	public DbConnectorCommand WithTimeout(TimeSpan timeSpan)
	{
		if (timeSpan <= TimeSpan.Zero && timeSpan != System.Threading.Timeout.InfiniteTimeSpan)
			throw new ArgumentOutOfRangeException(nameof(timeSpan), "Must be positive or 'Timeout.InfiniteTimeSpan'.");

		Timeout = timeSpan;
		return this;
	}

	public DbConnectorCommand WithParameter<T>(string key, T value) =>
		WithParameters(DbParameters.Create(key, value));

	public DbConnectorCommand WithParameters(DbParameters parameters)
	{
		m_parameters.Add(parameters);
		return this;
	}

	public DbConnectorCommand WithParameters(params IEnumerable<DbParameters> parameters) =>
		WithParameters(DbParameters.Create(parameters));

	public DbConnectorCommand WithParameters<T>(params IEnumerable<(string Name, T Value)> parameters) =>
		WithParameters(DbParameters.Create(parameters));

	public DbConnectorCommand WithParameters<T>(IEnumerable<KeyValuePair<string, T>> parameters) =>
		WithParameters(DbParameters.Create(parameters));

	public DbConnectorCommand WithParametersFromDto<T>(T dto, Func<string, bool>? where = null, Func<string, string>? renamed = null)
	{
		var parameters = DbParameters.FromDto(dto);
		if (where is not null)
			parameters = parameters.Where(where);
		if (renamed is not null)
			parameters = parameters.Renamed(renamed);
		return WithParameters(parameters);
	}

	/// <summary>
	/// Caches the command.
	/// </summary>
	public DbConnectorCommand Cache(bool cache = true)
	{
		IsCached = cache;
		return this;
	}

	/// <summary>
	/// Prepares the command.
	/// </summary>
	public DbConnectorCommand Prepare(bool prepare = true)
	{
		IsPrepared = prepare;
		return this;
	}

	internal DbConnectorCommand(DbConnector connector, string text, CommandType commandType)
	{
		Connector = connector;
		Text = text;
		CommandType = commandType;
		m_parameters = new DbParametersList();
	}

	private readonly DbParametersList m_parameters;
}
