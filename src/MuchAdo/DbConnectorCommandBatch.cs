using System.Data;

namespace MuchAdo;

/// <summary>
/// Encapsulates a database command or a batch of database commands.
/// </summary>
public sealed class DbConnectorCommandBatch
{
	/// <summary>
	/// The timeout.
	/// </summary>
	/// <remarks>If not specified, the default timeout is used.</remarks>
	public TimeSpan? Timeout { get; private set; }

	/// <summary>
	/// True after <see cref="Cache" /> is called.
	/// </summary>
	public bool? IsCached { get; private set; }

	/// <summary>
	/// True after <see cref="Prepare" /> is called.
	/// </summary>
	public bool? IsPrepared { get; private set; }

	/// <summary>
	/// The connector.
	/// </summary>
	public DbConnector Connector { get; }

	/// <summary>
	/// The number of queries in the command batch.
	/// </summary>
	public int CommandCount => m_textOrSql is null ? 0 : (1 + (m_batchedCommands?.Count ?? 0));

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
	public DbResultSetReader QueryMultiple() =>
		Connector.QueryMultiple(this);

	/// <summary>
	/// Executes the query, preparing to read multiple result sets.
	/// </summary>
	/// <seealso cref="QueryMultipleAsync" />
	public T QueryMultiple<T>(Func<DbResultSetReader, T> map) =>
		Connector.QueryMultiple(this, map ?? throw new ArgumentNullException(nameof(map)));

	/// <summary>
	/// Executes the query, preparing to read multiple result sets.
	/// </summary>
	/// <seealso cref="QueryMultiple" />
	public ValueTask<DbResultSetReader> QueryMultipleAsync(CancellationToken cancellationToken = default) =>
		Connector.QueryMultipleAsync(this, cancellationToken);

	/// <summary>
	/// Executes the query, preparing to read multiple result sets.
	/// </summary>
	/// <seealso cref="QueryMultiple" />
	public ValueTask<T> QueryMultipleAsync<T>(Func<DbResultSetReader, ValueTask<T>> map, CancellationToken cancellationToken = default) =>
		Connector.QueryMultipleAsync(this, map ?? throw new ArgumentNullException(nameof(map)), cancellationToken);

	/// <summary>
	/// Specifies that the query will be executed within an automatic transaction.
	/// </summary>
	public DbConnectorCommandBatch InTransaction() => InTransaction(Connector.DefaultTransactionSettings);

	/// <summary>
	/// Specifies that the query will be executed within an automatic transaction.
	/// </summary>
	public DbConnectorCommandBatch InTransaction(DbTransactionSettings settings)
	{
		InTransactionSettings = settings;
		return this;
	}

	/// <summary>
	/// Specifies that the query will be retried with the default retry policy.
	/// </summary>
	public DbConnectorCommandBatch Retry()
	{
		RetryPolicy = Connector.RetryPolicyOrThrow;
		return this;
	}

	/// <summary>
	/// Specifies that the query will be executed within an automatic transaction, which will be retried with the default retry policy.
	/// </summary>
	public DbConnectorCommandBatch RetryInTransaction() => Retry().InTransaction();

	/// <summary>
	/// Specifies that the query will be executed within an automatic transaction, which will be retried with the default retry policy.
	/// </summary>
	public DbConnectorCommandBatch RetryInTransaction(DbTransactionSettings settings) => Retry().InTransaction(settings);

	/// <summary>
	/// Sets the timeout.
	/// </summary>
	/// <remarks>Use <see cref="System.Threading.Timeout.InfiniteTimeSpan" /> (not <see cref="TimeSpan.Zero" />) for infinite timeout.</remarks>
	/// <exception cref="ArgumentOutOfRangeException"><c>timeSpan</c> is not positive or <see cref="System.Threading.Timeout.InfiniteTimeSpan" />.</exception>
	public DbConnectorCommandBatch WithTimeout(TimeSpan timeSpan)
	{
		if (timeSpan <= TimeSpan.Zero && timeSpan != System.Threading.Timeout.InfiniteTimeSpan)
			throw new ArgumentOutOfRangeException(nameof(timeSpan), "Must be positive or 'Timeout.InfiniteTimeSpan'.");

		Timeout = timeSpan;
		return this;
	}

	/// <summary>
	/// Caches the command batch.
	/// </summary>
	public DbConnectorCommandBatch Cache(bool cache = true)
	{
		IsCached = cache;
		return this;
	}

	/// <summary>
	/// Prepares the command batch.
	/// </summary>
	public DbConnectorCommandBatch Prepare(bool prepare = true)
	{
		IsPrepared = prepare;
		return this;
	}

	/// <summary>
	/// Adds a command.
	/// </summary>
	/// <param name="text">The text of the command.</param>
	public DbConnectorCommandBatch Command(string text) => StartNextCommand(CommandType.Text, text);

	/// <summary>
	/// Adds a command.
	/// </summary>
	/// <param name="text">The text of the command.</param>
	/// <param name="parameters">The parameters of the command.</param>
	public DbConnectorCommandBatch Command(string text, SqlParamSource parameters) => StartNextCommand(CommandType.Text, text, parameters);

	/// <summary>
	/// Adds a command.
	/// </summary>
	/// <param name="text">The text of the command.</param>
	/// <param name="parameters">The parameters of the command.</param>
	public DbConnectorCommandBatch Command(string text, params SqlParamSource[] parameters) => StartNextCommand(CommandType.Text, text, Sql.Combine(parameters));

	/// <summary>
	/// Adds a command from parameterized SQL.
	/// </summary>
	/// <param name="sql">The parameterized SQL.</param>
	public DbConnectorCommandBatch Command(SqlSource sql) => StartNextCommand(CommandType.Text, sql);

	/// <summary>
	/// Adds a command from parameterized SQL.
	/// </summary>
	/// <param name="sql">The parameterized SQL.</param>
	/// <param name="parameters">Additional parameters.</param>
	public DbConnectorCommandBatch Command(SqlSource sql, SqlParamSource parameters) => StartNextCommand(CommandType.Text, sql, parameters);

	/// <summary>
	/// Adds a command from parameterized SQL.
	/// </summary>
	/// <param name="sql">The parameterized SQL.</param>
	/// <param name="parameters">Additional parameters.</param>
	public DbConnectorCommandBatch Command(SqlSource sql, params SqlParamSource[] parameters) => StartNextCommand(CommandType.Text, sql, Sql.Combine(parameters));

	/// <summary>
	/// Adds a command from a formatted SQL string.
	/// </summary>
	/// <param name="sql">The formatted SQL string.</param>
	/// <remarks>Shorthand for <c>Command(Sql.Format(...))</c>.</remarks>
	public DbConnectorCommandBatch CommandFormat(SqlFormatStringHandler sql) => Command(Sql.Format(sql));

	/// <summary>
	/// Adds a command from a formatted SQL string.
	/// </summary>
	/// <param name="sql">The formatted SQL string.</param>
	/// <param name="parameters">Additional parameters.</param>
	/// <remarks>Shorthand for <c>Command(Sql.Format(...))</c>.</remarks>
	public DbConnectorCommandBatch CommandFormat(SqlFormatStringHandler sql, SqlParamSource parameters) => Command(Sql.Format(sql), parameters);

	/// <summary>
	/// Adds a command from a formatted SQL string.
	/// </summary>
	/// <param name="sql">The formatted SQL string.</param>
	/// <param name="parameters">Additional parameters.</param>
	public DbConnectorCommandBatch CommandFormat(SqlFormatStringHandler sql, params SqlParamSource[] parameters) => Command(Sql.Format(sql), parameters);

	/// <summary>
	/// Adds a command to access a stored procedure.
	/// </summary>
	/// <param name="name">The name of the stored procedure.</param>
	public DbConnectorCommandBatch StoredProcedure(string name) => StartNextCommand(CommandType.StoredProcedure, name);

	/// <summary>
	/// Adds a command to access a stored procedure.
	/// </summary>
	/// <param name="name">The name of the stored procedure.</param>
	/// <param name="parameters">The parameters of the stored procedure.</param>
	public DbConnectorCommandBatch StoredProcedure(string name, SqlParamSource parameters) => StartNextCommand(CommandType.StoredProcedure, name, parameters);

	/// <summary>
	/// Adds a command to access a stored procedure.
	/// </summary>
	/// <param name="name">The name of the stored procedure.</param>
	/// <param name="parameters">The parameters of the stored procedure.</param>
	public DbConnectorCommandBatch StoredProcedure(string name, params SqlParamSource[] parameters) => StartNextCommand(CommandType.StoredProcedure, name, Sql.Combine(parameters));

	/// <summary>
	/// Gets the command at the specified index.
	/// </summary>
	public DbConnectorCommand GetCommand(int index)
	{
		if (m_textOrSql is null)
			throw new ArgumentOutOfRangeException(nameof(index));

		if (index == (m_batchedCommands?.Count ?? 0))
			return new DbConnectorCommand(m_commandType, m_textOrSql, m_paramSource ?? Sql.Empty);

		if (m_batchedCommands is null)
			throw new ArgumentOutOfRangeException(nameof(index));

		return m_batchedCommands[index];
	}

	/// <summary>
	/// Replaces the command at the specified index.
	/// </summary>
	public DbConnectorCommandBatch SetCommand(int index, DbConnectorCommand command)
	{
		if (index == (m_batchedCommands?.Count ?? 0))
		{
			m_commandType = command.Type;
			m_textOrSql = command.TextOrSql;
			m_paramSource = command.Parameters;
		}
		else
		{
			if (m_batchedCommands is null)
				throw new ArgumentOutOfRangeException(nameof(index));

			m_batchedCommands[index] = command;
		}

		return this;
	}

	internal DbConnectorCommandBatch(DbConnector connector)
	{
		Connector = connector;
	}

	internal DbConnectorCommandBatch(DbConnector connector, CommandType commandType, object textOrSql, SqlParamSource? paramSource = null)
	{
		Connector = connector;
		m_commandType = commandType;
		m_textOrSql = textOrSql;
		m_paramSource = paramSource;
	}

	internal DbTransactionSettings? InTransactionSettings { get; private set; }

	internal DbRetryPolicy? RetryPolicy { get; private set; }

	private DbConnectorCommandBatch StartNextCommand(CommandType commandType, object textOrSql, SqlParamSource? paramSource = null)
	{
		if (m_textOrSql is null)
		{
			m_commandType = commandType;
			m_textOrSql = textOrSql;
			m_paramSource = paramSource;
		}
		else
		{
			m_batchedCommands ??= [];
			m_batchedCommands.Add(GetCommand(CommandCount - 1));

			m_commandType = commandType;
			m_textOrSql = textOrSql;
			m_paramSource = paramSource;
		}

		return this;
	}

	private CommandType m_commandType;
	private object? m_textOrSql;
	private SqlParamSource? m_paramSource;
	private List<DbConnectorCommand>? m_batchedCommands;
}
