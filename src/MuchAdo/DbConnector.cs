using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using MuchAdo.SqlFormatting;

namespace MuchAdo;

/// <summary>
/// Encapsulates a database connection and any current transaction.
/// </summary>
[SuppressMessage("Usage", "CA2213:Disposable fields should be disposed", Justification = "Fields are disposed indirectly.")]
public class DbConnector : IDisposable, IAsyncDisposable
{
	/// <summary>
	/// Creates a new DbConnector.
	/// </summary>
	/// <param name="connection">The database connection.</param>
	/// <param name="settings">The settings.</param>
	public DbConnector(IDbConnection connection, DbConnectorSettings? settings = null)
	{
		settings ??= s_defaultSettings;
		m_connection = connection ?? throw new ArgumentNullException(nameof(connection));
		m_isConnectionOpen = m_connection.State == ConnectionState.Open;
		m_noCloseConnection = m_isConnectionOpen;
		m_noDisposeConnection = settings.NoDispose;
		m_defaultIsolationLevel = settings.DefaultIsolationLevel;
		SqlSyntax = settings.SqlSyntax ?? SqlSyntax.Default;
		DataMapper = settings.DataMapper ?? DbDataMapper.Default;
	}

	/// <summary>
	/// The database connection.
	/// </summary>
	/// <remarks>Use <see cref="GetOpenConnectionAsync" /> or <see cref="GetOpenConnection" />
	/// to automatically open the connection.</remarks>
	public IDbConnection Connection => m_connection;

	/// <summary>
	/// The current transaction, if any.
	/// </summary>
	public IDbTransaction? Transaction => m_transaction;

	/// <summary>
	/// The SQL syntax used when formatting SQL.
	/// </summary>
	public SqlSyntax SqlSyntax { get; }

	/// <summary>
	/// Returns the database connection, opened if necessary.
	/// </summary>
	/// <returns>The opened database connection.</returns>
	/// <seealso cref="Connection" />
	/// <seealso cref="GetOpenConnectionAsync" />
	public IDbConnection GetOpenConnection()
	{
		VerifyNotDisposed();
		if (m_isConnectionOpen)
			return m_connection;

		OpenConnectionCore();
		m_isConnectionOpen = true;
		return m_connection;
	}

	/// <summary>
	/// Returns the database connection, opened if necessary.
	/// </summary>
	/// <param name="cancellationToken">The cancellation token.</param>
	/// <returns>The opened database connection.</returns>
	/// <seealso cref="Connection" />
	/// <seealso cref="GetOpenConnection" />
	public ValueTask<IDbConnection> GetOpenConnectionAsync(CancellationToken cancellationToken = default)
	{
		VerifyNotDisposed();
		return m_isConnectionOpen ? new ValueTask<IDbConnection>(m_connection) : DoAsync();

		async ValueTask<IDbConnection> DoAsync()
		{
			await OpenConnectionCoreAsync(cancellationToken).ConfigureAwait(false);
			m_isConnectionOpen = true;
			return m_connection;
		}
	}

	/// <summary>
	/// Opens the connection.
	/// </summary>
	/// <returns>An <see cref="IDisposable" /> that should be disposed when the connection should be closed.
	/// If the connection was already open, disposing the return value does nothing.</returns>
	/// <remarks>This method is not typically needed, since all operations automatically open
	/// the connection as needed.</remarks>
	/// <seealso cref="OpenConnectionAsync" />
	public DbConnectionCloser OpenConnection()
	{
		VerifyNotDisposed();
		if (m_isConnectionOpen)
			return default;

		OpenConnectionCore();
		m_isConnectionOpen = true;
		return new DbConnectionCloser(this);
	}

	/// <summary>
	/// Opens the connection.
	/// </summary>
	/// <param name="cancellationToken">The cancellation token.</param>
	/// <returns>An <see cref="IDisposable" /> that should be disposed when the connection should be closed.
	/// If the connection was already open, disposing the return value does nothing.</returns>
	/// <seealso cref="OpenConnection" />
	public ValueTask<DbConnectionCloser> OpenConnectionAsync(CancellationToken cancellationToken = default)
	{
		VerifyNotDisposed();
		return m_isConnectionOpen ? default : DoAsync();

		async ValueTask<DbConnectionCloser> DoAsync()
		{
			await OpenConnectionCoreAsync(cancellationToken).ConfigureAwait(false);
			m_isConnectionOpen = true;
			return new DbConnectionCloser(this);
		}
	}

	/// <summary>
	/// Begins a transaction.
	/// </summary>
	/// <returns>An <see cref="IDisposable" /> that should be disposed when the transaction has been committed or should be rolled back.</returns>
	/// <seealso cref="BeginTransactionAsync(CancellationToken)" />
	public DbTransactionDisposer BeginTransaction()
	{
		VerifyCanBeginTransaction();
		OpenConnection();
		m_transaction = m_defaultIsolationLevel is { } isolationLevel ? BeginTransactionCore(isolationLevel) : BeginTransactionCore();
		return new DbTransactionDisposer(this);
	}

	/// <summary>
	/// Begins a transaction.
	/// </summary>
	/// <param name="isolationLevel">The isolation level.</param>
	/// <returns>An <see cref="IDisposable" /> that should be disposed when the transaction has been committed or should be rolled back.</returns>
	/// <seealso cref="BeginTransactionAsync(IsolationLevel, CancellationToken)" />
	public DbTransactionDisposer BeginTransaction(IsolationLevel isolationLevel)
	{
		VerifyCanBeginTransaction();
		OpenConnection();
		m_transaction = BeginTransactionCore(isolationLevel);
		return new DbTransactionDisposer(this);
	}

	/// <summary>
	/// Begins a transaction.
	/// </summary>
	/// <param name="cancellationToken">The cancellation token.</param>
	/// <returns>An <see cref="IDisposable" /> that should be disposed when the transaction has been committed or should be rolled back.</returns>
	/// <seealso cref="BeginTransaction()" />
	public async ValueTask<DbTransactionDisposer> BeginTransactionAsync(CancellationToken cancellationToken = default)
	{
		VerifyCanBeginTransaction();
		await OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
		m_transaction = m_defaultIsolationLevel is { } isolationLevel
			? await BeginTransactionCoreAsync(isolationLevel, cancellationToken).ConfigureAwait(false)
			: await BeginTransactionCoreAsync(cancellationToken).ConfigureAwait(false);
		return new DbTransactionDisposer(this);
	}

	/// <summary>
	/// Begins a transaction.
	/// </summary>
	/// <param name="isolationLevel">The isolation level.</param>
	/// <param name="cancellationToken">The cancellation token.</param>
	/// <returns>An <see cref="IDisposable" /> that should be disposed when the transaction has been committed or should be rolled back.</returns>
	/// <seealso cref="BeginTransaction(IsolationLevel)" />
	public async ValueTask<DbTransactionDisposer> BeginTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken = default)
	{
		VerifyCanBeginTransaction();
		await OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
		m_transaction = await BeginTransactionCoreAsync(isolationLevel, cancellationToken).ConfigureAwait(false);
		return new DbTransactionDisposer(this);
	}

	/// <summary>
	/// Attaches a transaction.
	/// </summary>
	/// <returns>An <see cref="IDisposable" /> that should be disposed when the transaction has been committed or should be rolled back.</returns>
	public DbTransactionDisposer AttachTransaction(IDbTransaction transaction, bool noDispose = false)
	{
		if (!m_isConnectionOpen)
			throw new InvalidOperationException("The connection must be open to attach a transaction.");
		VerifyCanBeginTransaction();
		m_transaction = transaction;
		m_noDisposeTransaction = noDispose;
		return new DbTransactionDisposer(this);
	}

	/// <summary>
	/// Commits the current transaction.
	/// </summary>
	/// <seealso cref="CommitTransactionAsync" />
	public void CommitTransaction()
	{
		VerifyHasTransaction();
		CommitTransactionCore();
		DisposeTransaction();
	}

	/// <summary>
	/// Commits the current transaction.
	/// </summary>
	/// <param name="cancellationToken">The cancellation token.</param>
	/// <seealso cref="CommitTransaction" />
	public async ValueTask CommitTransactionAsync(CancellationToken cancellationToken = default)
	{
		VerifyHasTransaction();
		await CommitTransactionCoreAsync(cancellationToken).ConfigureAwait(false);
		await DisposeTransactionAsync().ConfigureAwait(false);
	}

	/// <summary>
	/// Rolls back the current transaction.
	/// </summary>
	/// <seealso cref="RollbackTransactionAsync" />
	public void RollbackTransaction()
	{
		VerifyHasTransaction();
		RollbackTransactionCore();
		DisposeTransaction();
	}

	/// <summary>
	/// Rolls back the current transaction.
	/// </summary>
	/// <param name="cancellationToken">The cancellation token.</param>
	/// <seealso cref="RollbackTransaction" />
	public async ValueTask RollbackTransactionAsync(CancellationToken cancellationToken = default)
	{
		VerifyHasTransaction();
		await RollbackTransactionCoreAsync(cancellationToken).ConfigureAwait(false);
		await DisposeTransactionAsync().ConfigureAwait(false);
	}

	/// <summary>
	/// Creates a new command.
	/// </summary>
	/// <param name="text">The text of the command.</param>
	public DbConnectorCommand Command(string text) => new(this, text, CommandType.Text);

	/// <summary>
	/// Creates a new command from parameterized SQL.
	/// </summary>
	/// <param name="sql">The parameterized SQL.</param>
	public DbConnectorCommand Command(Sql sql)
	{
		var (sqlText, sqlParameters) = SqlSyntax.Render(sql);
		return Command(sqlText).WithParameters(sqlParameters);
	}

	/// <summary>
	/// Creates a new command from a formatted SQL string.
	/// </summary>
	/// <param name="sql">The formatted SQL string.</param>
	/// <remarks>Shorthand for <c>Command(Sql.Format($"..."))</c>.</remarks>
	public DbConnectorCommand CommandFormat(SqlFormatStringHandler sql) => Command(Sql.Format(sql));

	/// <summary>
	/// Creates a new command to access a stored procedure.
	/// </summary>
	/// <param name="name">The name of the stored procedure.</param>
	public DbConnectorCommand StoredProcedure(string name) => new(this, name, CommandType.StoredProcedure);

	/// <summary>
	/// Closes the connection.
	/// </summary>
	/// <remarks>This method closes the underlying connection, which will be automatically reopened it if it is used again.</remarks>
	public void CloseConnection()
	{
		VerifyNotDisposed();

		if (!m_isConnectionOpen || m_noCloseConnection)
			return;

		CloseConnectionCore();
		m_isConnectionOpen = false;
	}

	/// <summary>
	/// Closes the connection.
	/// </summary>
	/// <remarks>This method closes the underlying connection, which will be automatically reopened it if it is used again.</remarks>
	public ValueTask CloseConnectionAsync()
	{
		VerifyNotDisposed();

		if (!m_isConnectionOpen || m_noCloseConnection)
			return default;

		return DoAsync();

		async ValueTask DoAsync()
		{
			await CloseConnectionCoreAsync().ConfigureAwait(false);
			m_isConnectionOpen = false;
		}
	}

	/// <summary>
	/// Attaches a disposable to the connector, which is disposed when the connector is disposed.
	/// </summary>
	public void AttachDisposable(object disposable)
	{
		if (!m_disposable.IsDefault)
			throw new InvalidOperationException("A disposable is already attached.");

		m_disposable = new AsyncScope(disposable);
	}

	/// <summary>
	/// Disposes the connector.
	/// </summary>
	/// <seealso cref="DisposeAsync" />
	public void Dispose()
	{
		if (ConnectorPool is not null)
		{
			DisposeTransaction();
			ConnectorPool.ReturnConnector(this);
			return;
		}

		if (m_isDisposed)
			return;

		DisposeTransaction();
		DisposeCachedCommands();
		if (!m_noDisposeConnection)
			m_connection.Dispose();
		DisposeConnectionCore();
		m_isDisposed = true;
	}

	/// <summary>
	/// Disposes the connector.
	/// </summary>
	/// <seealso cref="Dispose" />
	public ValueTask DisposeAsync()
	{
		if (ConnectorPool is not null)
		{
			ConnectorPool.ReturnConnector(this);
			ConnectorPool = null;
			return default;
		}

		return m_isDisposed ? default : DoAsync();

		async ValueTask DoAsync()
		{
			await DisposeTransactionAsync().ConfigureAwait(false);
			await DisposeCachedCommandsAsync().ConfigureAwait(false);
			if (!m_noDisposeConnection)
				await DisposeConnectionCoreAsync().ConfigureAwait(false);
			await m_disposable.DisposeAsync().ConfigureAwait(false);
			m_isDisposed = true;
		}
	}

	/// <summary>
	/// Opens the connection.
	/// </summary>
	protected virtual void OpenConnectionCore() => Connection.Open();

	/// <summary>
	/// Opens the connection asynchronously.
	/// </summary>
	protected virtual ValueTask OpenConnectionCoreAsync(CancellationToken cancellationToken)
	{
		if (Connection is DbConnection dbConnection)
			return new ValueTask(dbConnection.OpenAsync(cancellationToken));

		Connection.Open();
		return default;
	}

	/// <summary>
	/// Closes a connection.
	/// </summary>
	protected virtual void CloseConnectionCore() => Connection.Close();

	/// <summary>
	/// Closes a connection asynchronously.
	/// </summary>
	protected virtual ValueTask CloseConnectionCoreAsync()
	{
#if !NETSTANDARD2_0
		if (Connection is DbConnection dbConnection)
			return new ValueTask(dbConnection.CloseAsync());
#endif

		Connection.Close();
		return default;
	}

	/// <summary>
	/// Disposes a connection.
	/// </summary>
	protected virtual void DisposeConnectionCore() => Connection.Dispose();

	/// <summary>
	/// Disposes a connection asynchronously.
	/// </summary>
	protected virtual ValueTask DisposeConnectionCoreAsync()
	{
#if !NETSTANDARD2_0
		if (Connection is DbConnection dbConnection)
			return dbConnection.DisposeAsync();
#endif

		Connection.Dispose();
		return default;
	}

	/// <summary>
	/// Begins a transaction.
	/// </summary>
	protected virtual IDbTransaction BeginTransactionCore() => Connection.BeginTransaction();

	/// <summary>
	/// Begins a transaction asynchronously.
	/// </summary>
	protected virtual ValueTask<IDbTransaction> BeginTransactionCoreAsync(CancellationToken cancellationToken)
	{
#if !NETSTANDARD2_0
		if (Connection is DbConnection dbConnection)
		{
			static async ValueTask<IDbTransaction> DoAsync(DbConnection c, CancellationToken ct) =>
				await c.BeginTransactionAsync(ct).ConfigureAwait(false);

			return DoAsync(dbConnection, cancellationToken);
		}
#endif

		return new ValueTask<IDbTransaction>(Connection.BeginTransaction());
	}

	/// <summary>
	/// Begins a transaction.
	/// </summary>
	protected virtual IDbTransaction BeginTransactionCore(IsolationLevel isolationLevel) => Connection.BeginTransaction(isolationLevel);

	/// <summary>
	/// Begins a transaction asynchronously.
	/// </summary>
	protected virtual ValueTask<IDbTransaction> BeginTransactionCoreAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken)
	{
#if !NETSTANDARD2_0
		if (Connection is DbConnection dbConnection)
		{
			static async ValueTask<IDbTransaction> DoAsync(DbConnection c, IsolationLevel il, CancellationToken ct) =>
				await c.BeginTransactionAsync(il, ct).ConfigureAwait(false);

			return DoAsync(dbConnection, isolationLevel, cancellationToken);
		}
#endif

		return new ValueTask<IDbTransaction>(Connection.BeginTransaction(isolationLevel));
	}

	/// <summary>
	/// Commits a transaction.
	/// </summary>
	protected virtual void CommitTransactionCore() => Transaction!.Commit();

	/// <summary>
	/// Commits a transaction asynchronously.
	/// </summary>
	protected virtual ValueTask CommitTransactionCoreAsync(CancellationToken cancellationToken)
	{
#if !NETSTANDARD2_0
		if (Transaction! is DbTransaction dbTransaction)
			return new ValueTask(dbTransaction.CommitAsync(cancellationToken));
#endif

		Transaction!.Commit();
		return default;
	}

	/// <summary>
	/// Rolls back a transaction.
	/// </summary>
	protected virtual void RollbackTransactionCore() => Transaction!.Rollback();

	/// <summary>
	/// Rolls back a transaction asynchronously.
	/// </summary>
	protected virtual ValueTask RollbackTransactionCoreAsync(CancellationToken cancellationToken)
	{
#if !NETSTANDARD2_0
		if (Transaction! is DbTransaction dbTransaction)
			return new ValueTask(dbTransaction.RollbackAsync(cancellationToken));
#endif

		Transaction!.Rollback();
		return default;
	}

	/// <summary>
	/// Disposes a transaction.
	/// </summary>
	protected virtual void DisposeTransactionCore() => Transaction!.Dispose();

	/// <summary>
	/// Disposes a transaction asynchronously.
	/// </summary>
	protected virtual ValueTask DisposeTransactionCoreAsync()
	{
#if !NETSTANDARD2_0
		if (Transaction! is DbTransaction dbTransaction)
			return dbTransaction.DisposeAsync();
#endif

		Transaction!.Dispose();
		return default;
	}

	/// <summary>
	/// Executes a non-query command.
	/// </summary>
	protected internal virtual int ExecuteNonQueryCore() => ActiveCommand.ExecuteNonQuery();

	/// <summary>
	/// Executes a non-query command asynchronously.
	/// </summary>
	protected internal virtual ValueTask<int> ExecuteNonQueryCoreAsync(CancellationToken cancellationToken)
	{
		if (ActiveCommand is DbCommand dbCommand)
			return new ValueTask<int>(dbCommand.ExecuteNonQueryAsync(cancellationToken));

		return new ValueTask<int>(ActiveCommand.ExecuteNonQuery());
	}

	/// <summary>
	/// Executes a command query.
	/// </summary>
	protected internal virtual IDataReader ExecuteReaderCore() => ActiveCommand.ExecuteReader();

	/// <summary>
	/// Executes a command query asynchronously.
	/// </summary>
	protected internal virtual ValueTask<IDataReader> ExecuteReaderCoreAsync(CancellationToken cancellationToken)
	{
		if (ActiveCommand is DbCommand dbCommand)
		{
			static async ValueTask<IDataReader> DoAsync(DbCommand c, CancellationToken ct) =>
				await c.ExecuteReaderAsync(ct).ConfigureAwait(false);

			return DoAsync(dbCommand, cancellationToken);
		}

		return new ValueTask<IDataReader>(ActiveCommand.ExecuteReader());
	}

	/// <summary>
	/// Executes a command query.
	/// </summary>
	protected internal virtual IDataReader ExecuteReaderCore(CommandBehavior commandBehavior) => ActiveCommand.ExecuteReader(commandBehavior);

	/// <summary>
	/// Executes a command query asynchronously.
	/// </summary>
	protected internal virtual ValueTask<IDataReader> ExecuteReaderCoreAsync(CommandBehavior commandBehavior, CancellationToken cancellationToken)
	{
		if (ActiveCommand is DbCommand dbCommand)
		{
			static async ValueTask<IDataReader> DoAsync(DbCommand c, CommandBehavior cb, CancellationToken ct) =>
				await c.ExecuteReaderAsync(cb, ct).ConfigureAwait(false);

			return DoAsync(dbCommand, commandBehavior, cancellationToken);
		}

		return new ValueTask<IDataReader>(ActiveCommand.ExecuteReader(commandBehavior));
	}

	/// <summary>
	/// Prepares a command.
	/// </summary>
	protected internal virtual void PrepareCommandCore() => ActiveCommand.Prepare();

	/// <summary>
	/// Prepares a command asynchronously.
	/// </summary>
	protected internal virtual ValueTask PrepareCommandCoreAsync(CancellationToken cancellationToken)
	{
#if !NETSTANDARD2_0
		if (ActiveCommand is DbCommand dbCommand)
			return new ValueTask(dbCommand.PrepareAsync(cancellationToken));
#endif

		ActiveCommand.Prepare();
		return default;
	}

	/// <summary>
	/// Disposes a command.
	/// </summary>
	protected virtual void DisposeCommandCore() => ActiveCommand.Dispose();

	/// <summary>
	/// Disposes a command asynchronously.
	/// </summary>
	protected internal virtual ValueTask DisposeCommandCoreAsync()
	{
#if !NETSTANDARD2_0
		if (ActiveCommand is DbCommand dbCommand)
			return dbCommand.DisposeAsync();
#endif

		ActiveCommand.Dispose();
		return default;
	}

	/// <summary>
	/// Reads the next record.
	/// </summary>
	protected internal virtual bool ReadReaderCore() => ActiveReader.Read();

	/// <summary>
	/// Reads the next record asynchronously.
	/// </summary>
	protected internal virtual ValueTask<bool> ReadReaderCoreAsync(CancellationToken cancellationToken)
	{
		if (ActiveReader is DbDataReader dbReader)
			return new ValueTask<bool>(dbReader.ReadAsync(cancellationToken));

		return new ValueTask<bool>(ActiveReader.Read());
	}

	/// <summary>
	/// Reads the next result.
	/// </summary>
	protected internal virtual bool NextReaderResultCore() => ActiveReader.NextResult();

	/// <summary>
	/// Reads the next result asynchronously.
	/// </summary>
	protected internal virtual ValueTask<bool> NextReaderResultCoreAsync(CancellationToken cancellationToken)
	{
		if (ActiveReader is DbDataReader dbReader)
			return new ValueTask<bool>(dbReader.NextResultAsync(cancellationToken));

		return new ValueTask<bool>(ActiveReader.NextResult());
	}

	/// <summary>
	/// Disposes a reader.
	/// </summary>
	protected virtual void DisposeReaderCore() => ActiveReader.Dispose();

	/// <summary>
	/// Disposes a reader asynchronously.
	/// </summary>
	protected internal virtual ValueTask DisposeReaderCoreAsync()
	{
#if !NETSTANDARD2_0
		if (ActiveReader is DbDataReader dbReader)
			return dbReader.DisposeAsync();
#endif

		ActiveReader.Dispose();
		return default;
	}

	protected internal virtual IDbCommand CreateCommandCore() => Connection.CreateCommand();

	/// <summary>
	/// Creates a parameter with the specified name and value.
	/// </summary>
	protected internal virtual IDataParameter CreateParameterCore<T>(string name, T value)
	{
		var parameter = ActiveCommand.CreateParameter();
		parameter.ParameterName = name;
		parameter.Value = value is null ? DBNull.Value : value;
		return parameter;
	}

	/// <summary>
	/// Updates the parameter value of a parameter.
	/// </summary>
	protected internal virtual void SetParameterValueCore<T>(IDataParameter parameter, T value)
	{
		parameter.Value = value switch
		{
			null => DBNull.Value,
			IDataParameter ddp => ddp.Value,
			_ => value,
		};
	}

	internal DbDataMapper DataMapper { get; }

	internal DbCommandCache CommandCache => m_commandCache ??= new();

	internal DbConnectorPool? ConnectorPool { get; set; }

	internal void DisposeTransaction()
	{
		VerifyNotDisposed();

		if (m_transaction is not null)
		{
			if (!m_noDisposeTransaction)
				DisposeTransactionCore();
			m_transaction = null;
		}
	}

	internal async ValueTask DisposeTransactionAsync()
	{
		VerifyNotDisposed();

		if (m_transaction is not null)
		{
			if (!m_noDisposeTransaction)
				await DisposeTransactionCoreAsync().ConfigureAwait(false);
			m_transaction = null;
		}
	}

	protected internal IDbCommand ActiveCommand => m_activeCommand ?? throw new InvalidOperationException("No active command available.");

	internal void SetActiveCommand(IDbCommand command, bool isCached)
	{
		m_activeCommand = command;
		m_activeCommandIsCached = isCached;
	}

	protected internal IDataReader ActiveReader => m_activeReader ?? throw new InvalidOperationException("No active reader available.");

	internal void SetActiveReader(IDataReader reader) => m_activeReader = reader;

	internal void DisposeActiveCommand()
	{
		VerifyNotDisposed();

		if (m_activeCommand is not null)
		{
			if (!m_activeCommandIsCached)
				DisposeCommandCore();
			m_activeCommand = null;
		}
	}

	internal async ValueTask DisposeActiveCommandAsync()
	{
		VerifyNotDisposed();

		if (m_activeCommand is not null)
		{
			if (!m_activeCommandIsCached)
				await DisposeCommandCoreAsync().ConfigureAwait(false);
			m_activeCommand = null;
		}
	}

	internal void DisposeActiveReader()
	{
		VerifyNotDisposed();

		if (m_activeReader is not null)
		{
			DisposeReaderCore();
			m_activeReader = null;
		}
	}

	internal async ValueTask DisposeActiveReaderAsync()
	{
		VerifyNotDisposed();

		if (m_activeReader is not null)
		{
			await DisposeReaderCoreAsync().ConfigureAwait(false);
			m_activeReader = null;
		}
	}

	private void DisposeCachedCommands()
	{
		if (m_commandCache is null)
			return;

		var commands = m_commandCache.GetCommands();
		foreach (var command in commands)
		{
			m_activeCommand = command;
			DisposeCommandCore();
		}
		m_activeCommand = null;
	}

	private ValueTask DisposeCachedCommandsAsync()
	{
		if (m_commandCache is null)
			return default;

		var commands = m_commandCache.GetCommands();
		return commands.Count != 0 ? DoAsync() : default;

		async ValueTask DoAsync()
		{
			foreach (var command in commands)
			{
				m_activeCommand = command;
				await DisposeCommandCoreAsync().ConfigureAwait(false);
			}
			m_activeCommand = null;
		}
	}

	private void VerifyNotDisposed()
	{
		if (m_isDisposed)
			throw new ObjectDisposedException(typeof(DbConnector).ToString());
	}

	private void VerifyCanBeginTransaction()
	{
		VerifyNotDisposed();

		if (Transaction is not null)
			throw new InvalidOperationException("A transaction is already started.");
	}

	private void VerifyHasTransaction()
	{
		VerifyNotDisposed();

		if (Transaction is null)
			throw new InvalidOperationException("No transaction available; call BeginTransaction first.");
	}

	private static readonly DbConnectorSettings s_defaultSettings = new();

	private readonly bool m_noDisposeConnection;
	private readonly bool m_noCloseConnection;
	private readonly IsolationLevel? m_defaultIsolationLevel;
	private readonly IDbConnection m_connection;
	private IDbTransaction? m_transaction;
	private IDbCommand? m_activeCommand;
	private IDataReader? m_activeReader;
	private DbCommandCache? m_commandCache;
	private AsyncScope m_disposable;
	private bool m_isConnectionOpen;
	private bool m_isDisposed;
	private bool m_noDisposeTransaction;
	private bool m_activeCommandIsCached;
}
