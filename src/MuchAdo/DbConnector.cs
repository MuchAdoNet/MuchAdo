using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace MuchAdo;

/// <summary>
/// Encapsulates a database connection and any current transaction.
/// </summary>
[SuppressMessage("Usage", "CA2213:Disposable fields should be disposed", Justification = "Fields are disposed indirectly.")]
public class DbConnector : IDisposable, IAsyncDisposable
{
	/// <summary>
	/// Creates a new connector.
	/// </summary>
	/// <param name="connection">The database connection.</param>
	public DbConnector(IDbConnection connection)
		: this(connection, DbConnectorSettings.Default)
	{
	}

	/// <summary>
	/// Creates a new connector.
	/// </summary>
	/// <param name="connection">The database connection.</param>
	/// <param name="settings">The settings.</param>
	public DbConnector(IDbConnection connection, DbConnectorSettings settings)
	{
		m_connection = connection ?? throw new ArgumentNullException(nameof(connection));
		Settings = settings ?? throw new ArgumentNullException(nameof(settings));
		m_isConnectionOpen = m_connection.State == ConnectionState.Open;
		m_noCloseConnection = m_isConnectionOpen;
	}

	/// <summary>
	/// The database connection.
	/// </summary>
	public IDbConnection Connection => m_connection;

	/// <summary>
	/// The current transaction, if any.
	/// </summary>
	public IDbTransaction? Transaction => m_transaction;

	/// <summary>
	/// The active command, if any.
	/// </summary>
	public IDbCommand? ActiveCommand => m_activeCommandOrBatch as IDbCommand;

#if NET
	/// <summary>
	/// The active batch, if any.
	/// </summary>
	public DbBatch? ActiveBatch => m_activeCommandOrBatch as DbBatch;
#endif

	/// <summary>
	/// The active reader, if any.
	/// </summary>
	public IDataReader? ActiveReader => m_activeReader;

	/// <summary>
	/// The SQL syntax used when formatting SQL.
	/// </summary>
	public SqlSyntax SqlSyntax => Settings.SqlSyntax;

	/// <summary>
	/// Raised immediately before a command batch is executed.
	/// </summary>
	public event EventHandler<DbConnectorExecutingEventArgs>? Executing;

	/// <summary>
	/// Creates a new command.
	/// </summary>
	/// <param name="text">The text of the command.</param>
	public DbConnectorCommandBatch Command(string text) =>
		new(this, CommandType.Text, text ?? throw new ArgumentNullException(nameof(text)));

	/// <summary>
	/// Creates a new command.
	/// </summary>
	/// <param name="text">The text of the command.</param>
	/// <param name="parameters">The parameters of the command.</param>
	public DbConnectorCommandBatch Command(string text, SqlParamSource parameters) =>
		new(this, CommandType.Text, text ?? throw new ArgumentNullException(nameof(text)), parameters ?? throw new ArgumentNullException(nameof(parameters)));

	/// <summary>
	/// Creates a new command.
	/// </summary>
	/// <param name="text">The text of the command.</param>
	/// <param name="parameters">The parameters of the command.</param>
	public DbConnectorCommandBatch Command(string text, params ReadOnlySpan<SqlParamSource> parameters) =>
		new(this, CommandType.Text, text ?? throw new ArgumentNullException(nameof(text)), new SqlParamSourceList(parameters));

	/// <summary>
	/// Creates a new command from parameterized SQL.
	/// </summary>
	/// <param name="sql">The parameterized SQL.</param>
	public DbConnectorCommandBatch Command(SqlSource sql) =>
		new(this, CommandType.Text, sql ?? throw new ArgumentNullException(nameof(sql)));

	/// <summary>
	/// Creates a new command from parameterized SQL.
	/// </summary>
	/// <param name="sql">The parameterized SQL.</param>
	/// <param name="parameters">The parameters of the command.</param>
	public DbConnectorCommandBatch Command(SqlSource sql, SqlParamSource parameters) =>
		new(this, CommandType.Text, sql ?? throw new ArgumentNullException(nameof(sql)), parameters ?? throw new ArgumentNullException(nameof(parameters)));

	/// <summary>
	/// Creates a new command from parameterized SQL.
	/// </summary>
	/// <param name="sql">The parameterized SQL.</param>
	/// <param name="parameters">The parameters of the command.</param>
	public DbConnectorCommandBatch Command(SqlSource sql, params ReadOnlySpan<SqlParamSource> parameters) =>
		new(this, CommandType.Text, sql ?? throw new ArgumentNullException(nameof(sql)), new SqlParamSourceList(parameters));

	/// <summary>
	/// Creates a new command from a formatted SQL string.
	/// </summary>
	/// <param name="sql">The formatted SQL string.</param>
	/// <remarks>Shorthand for <c>Command(Sql.Format(...))</c>.</remarks>
	public DbConnectorCommandBatch CommandFormat(SqlFormatStringHandler sql) =>
		Command(Sql.Format(sql));

	/// <summary>
	/// Creates a new command from a formatted SQL string.
	/// </summary>
	/// <param name="sql">The formatted SQL string.</param>
	/// <param name="parameters">The parameters of the command.</param>
	public DbConnectorCommandBatch CommandFormat(SqlFormatStringHandler sql, SqlParamSource parameters) =>
		Command(Sql.Format(sql), parameters);

	/// <summary>
	/// Creates a new command from a formatted SQL string.
	/// </summary>
	/// <param name="sql">The formatted SQL string.</param>
	/// <param name="parameters">The parameters of the command.</param>
	public DbConnectorCommandBatch CommandFormat(SqlFormatStringHandler sql, params ReadOnlySpan<SqlParamSource> parameters) =>
		Command(Sql.Format(sql), parameters);

	/// <summary>
	/// Creates a new command to access a stored procedure.
	/// </summary>
	/// <param name="name">The name of the stored procedure.</param>
	public DbConnectorCommandBatch StoredProcedure(string name) =>
		new(this, CommandType.StoredProcedure, name ?? throw new ArgumentNullException(nameof(name)));

	/// <summary>
	/// Creates a new command to access a stored procedure.
	/// </summary>
	/// <param name="name">The name of the stored procedure.</param>
	/// <param name="parameters">The parameters of the stored procedure.</param>
	public DbConnectorCommandBatch StoredProcedure(string name, SqlParamSource parameters) =>
		new(this, CommandType.StoredProcedure, name ?? throw new ArgumentNullException(nameof(name)), parameters ?? throw new ArgumentNullException(nameof(parameters)));

	/// <summary>
	/// Creates a new command to access a stored procedure.
	/// </summary>
	/// <param name="name">The name of the stored procedure.</param>
	/// <param name="parameters">The parameters of the stored procedure.</param>
	public DbConnectorCommandBatch StoredProcedure(string name, params ReadOnlySpan<SqlParamSource> parameters) =>
		new(this, CommandType.StoredProcedure, name ?? throw new ArgumentNullException(nameof(name)), new SqlParamSourceList(parameters));

	/// <summary>
	/// Begins a transaction.
	/// </summary>
	/// <returns>An <see cref="IDisposable" /> that should be disposed when the transaction has been committed or should be rolled back.</returns>
	/// <seealso cref="BeginTransactionAsync(CancellationToken)" />
	public DbTransactionDisposer BeginTransaction()
	{
		VerifyCanBeginTransaction();
		OpenConnection();
		m_transaction = Settings.DefaultIsolationLevel is { } isolationLevel ? BeginTransactionCore(isolationLevel) : BeginTransactionCore();
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
		m_transaction = Settings.DefaultIsolationLevel is { } isolationLevel
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
	/// Returns the database connection, opened if necessary.
	/// </summary>
	/// <returns>The opened database connection.</returns>
	/// <remarks>This method is not typically needed, since the connection is opened automatically
	/// immediately before a command is executed and remains open until the connector is disposed.</remarks>
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
	/// <remarks>This method is not typically needed, since the connection is opened automatically
	/// immediately before a command is executed and remains open until the connector is disposed.</remarks>
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
	/// <remarks>This method is not typically needed, since the connection is opened automatically
	/// immediately before a command is executed and remains open until the connector is disposed.</remarks>
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
	/// <remarks>This method is not typically needed, since the connection is opened automatically
	/// immediately before a command is executed and remains open until the connector is disposed.</remarks>
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
	/// Cancels the active command or batch.
	/// </summary>
	public void Cancel()
	{
		if (ActiveCommandOrBatch is null)
			throw new InvalidOperationException("No command or batch is currently active.");

		CancelCore();
	}

	/// <summary>
	/// Attaches a transaction.
	/// </summary>
	/// <param name="transaction">The transaction to attach.</param>
	/// <param name="noDispose">If true, the transaction is not disposed by the connector.</param>
	/// <returns>An <see cref="IDisposable" /> that should be disposed when the transaction has been committed or should be rolled back.</returns>
	/// <remarks>The connection must be open to attach a transaction; first call <c>OpenTransaction</c> or <c>OpenTransactionAsync</c>.</remarks>
	public DbTransactionDisposer AttachTransaction(IDbTransaction transaction, bool noDispose = false)
	{
		if (!m_isConnectionOpen)
			throw new InvalidOperationException("The connection must be open to attach a transaction; first call OpenTransaction or OpenTransactionAsync.");

		VerifyCanBeginTransaction();
		m_transaction = transaction ?? throw new ArgumentNullException(nameof(transaction));
		m_noDisposeTransaction = noDispose;
		return new DbTransactionDisposer(this);
	}

	/// <summary>
	/// Attaches a disposable to the connector, which is disposed when the connector is disposed.
	/// </summary>
	public void AttachDisposable(object disposable) => (m_disposables ??= []).Add(disposable);

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
		if (!Settings.NoDisposeConnection)
			DisposeConnectionCore();
		DisposeDisposables();
		m_isDisposed = true;
	}

	/// <summary>
	/// Disposes the connector.
	/// </summary>
	/// <seealso cref="Dispose" />
	public async ValueTask DisposeAsync()
	{
		if (ConnectorPool is not null)
		{
			await DisposeTransactionAsync().ConfigureAwait(false);
			ConnectorPool.ReturnConnector(this);
			return;
		}

		if (m_isDisposed)
			return;

		await DisposeTransactionAsync().ConfigureAwait(false);
		await DisposeCachedCommandsAsync().ConfigureAwait(false);
		if (!Settings.NoDisposeConnection)
			await DisposeConnectionCoreAsync().ConfigureAwait(false);
		await DisposeDisposablesAsync().ConfigureAwait(false);
		m_isDisposed = true;
	}

	/// <summary>
	/// The connector settings.
	/// </summary>
	protected DbConnectorSettings Settings { get; }

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
	/// Closes the connection.
	/// </summary>
	protected virtual void CloseConnectionCore() => Connection.Close();

	/// <summary>
	/// Closes the connection asynchronously.
	/// </summary>
	protected virtual ValueTask CloseConnectionCoreAsync()
	{
#if NET
		if (Connection is DbConnection dbConnection)
			return new ValueTask(dbConnection.CloseAsync());
#endif

		Connection.Close();
		return default;
	}

	/// <summary>
	/// Disposes the connection.
	/// </summary>
	protected virtual void DisposeConnectionCore() => Connection.Dispose();

	/// <summary>
	/// Disposes the connection asynchronously.
	/// </summary>
	protected virtual ValueTask DisposeConnectionCoreAsync()
	{
#if NET
		if (Connection is DbConnection dbConnection)
			return dbConnection.DisposeAsync();
#endif

		DisposeConnectionCore();
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
#if NET
		if (Connection is DbConnection dbConnection)
		{
			static async ValueTask<IDbTransaction> DoAsync(DbConnection c, CancellationToken ct) =>
				await c.BeginTransactionAsync(ct).ConfigureAwait(false);

			return DoAsync(dbConnection, cancellationToken);
		}
#endif

		return new ValueTask<IDbTransaction>(BeginTransactionCore());
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
#if NET
		if (Connection is DbConnection dbConnection)
		{
			static async ValueTask<IDbTransaction> DoAsync(DbConnection c, IsolationLevel il, CancellationToken ct) =>
				await c.BeginTransactionAsync(il, ct).ConfigureAwait(false);

			return DoAsync(dbConnection, isolationLevel, cancellationToken);
		}
#endif

		return new ValueTask<IDbTransaction>(BeginTransactionCore(isolationLevel));
	}

	/// <summary>
	/// Commits the current transaction.
	/// </summary>
	protected virtual void CommitTransactionCore() => Transaction!.Commit();

	/// <summary>
	/// Commits the current transaction asynchronously.
	/// </summary>
	protected virtual ValueTask CommitTransactionCoreAsync(CancellationToken cancellationToken)
	{
#if NET
		if (Transaction! is DbTransaction dbTransaction)
			return new ValueTask(dbTransaction.CommitAsync(cancellationToken));
#endif

		CommitTransactionCore();
		return default;
	}

	/// <summary>
	/// Rolls back the current transaction.
	/// </summary>
	protected virtual void RollbackTransactionCore() => Transaction!.Rollback();

	/// <summary>
	/// Rolls back a current transaction asynchronously.
	/// </summary>
	protected virtual ValueTask RollbackTransactionCoreAsync(CancellationToken cancellationToken)
	{
#if NET
		if (Transaction! is DbTransaction dbTransaction)
			return new ValueTask(dbTransaction.RollbackAsync(cancellationToken));
#endif

		RollbackTransactionCore();
		return default;
	}

	/// <summary>
	/// Disposes the current transaction.
	/// </summary>
	protected virtual void DisposeTransactionCore() => Transaction!.Dispose();

	/// <summary>
	/// Disposes the current transaction asynchronously.
	/// </summary>
	protected virtual ValueTask DisposeTransactionCoreAsync()
	{
#if NET
		if (Transaction! is DbTransaction dbTransaction)
			return dbTransaction.DisposeAsync();
#endif

		DisposeTransactionCore();
		return default;
	}

	/// <summary>
	/// The active command or batch, if any, i.e. an <c>IDbCommand</c> or a <c>DbBatch</c>.
	/// </summary>
	protected object? ActiveCommandOrBatch => m_activeCommandOrBatch;

	/// <summary>
	/// Executes the active command or batch.
	/// </summary>
	protected virtual int ExecuteNonQueryCore()
	{
		if (ActiveCommandOrBatch is IDbCommand command)
			return command.ExecuteNonQuery();

#if NET
		if (ActiveCommandOrBatch is DbBatch batch)
			return batch.ExecuteNonQuery();
#endif

		throw new NotSupportedException();
	}

	/// <summary>
	/// Executes the active command or batch asynchronously.
	/// </summary>
	protected virtual ValueTask<int> ExecuteNonQueryCoreAsync(CancellationToken cancellationToken)
	{
		if (ActiveCommandOrBatch is DbCommand command)
			return new ValueTask<int>(command.ExecuteNonQueryAsync(cancellationToken));

#if NET
		if (ActiveCommandOrBatch is DbBatch batch)
			return new ValueTask<int>(batch.ExecuteNonQueryAsync(cancellationToken));
#endif

		return new ValueTask<int>(ExecuteNonQueryCore());
	}

	/// <summary>
	/// Opens a reader for the active command or batch.
	/// </summary>
	protected virtual IDataReader ExecuteReaderCore()
	{
		if (ActiveCommandOrBatch is IDbCommand command)
			return command.ExecuteReader();

#if NET
		if (ActiveCommandOrBatch is DbBatch batch)
			return batch.ExecuteReader();
#endif

		throw new NotSupportedException();
	}

	/// <summary>
	/// Opens a reader for the active command or batch asynchronously.
	/// </summary>
	protected virtual ValueTask<IDataReader> ExecuteReaderCoreAsync(CancellationToken cancellationToken)
	{
		if (ActiveCommandOrBatch is DbCommand command)
		{
			static async ValueTask<IDataReader> DoAsync(DbCommand c, CancellationToken ct) =>
				await c.ExecuteReaderAsync(ct).ConfigureAwait(false);

			return DoAsync(command, cancellationToken);
		}

#if NET
		if (ActiveCommandOrBatch is DbBatch batch)
		{
			static async ValueTask<IDataReader> DoAsync(DbBatch b, CancellationToken ct) =>
				await b.ExecuteReaderAsync(ct).ConfigureAwait(false);

			return DoAsync(batch, cancellationToken);
		}
#endif

		return new ValueTask<IDataReader>(ExecuteReaderCore());
	}

	/// <summary>
	/// Opens a reader for the active command or batch.
	/// </summary>
	protected virtual IDataReader ExecuteReaderCore(CommandBehavior commandBehavior)
	{
		if (ActiveCommandOrBatch is IDbCommand command)
			return command.ExecuteReader(commandBehavior);

#if NET
		if (ActiveCommandOrBatch is DbBatch batch)
			return batch.ExecuteReader(commandBehavior);
#endif

		throw new NotSupportedException();
	}

	/// <summary>
	/// Opens a reader for the active command or batch asynchronously.
	/// </summary>
	protected virtual ValueTask<IDataReader> ExecuteReaderCoreAsync(CommandBehavior commandBehavior, CancellationToken cancellationToken)
	{
		if (ActiveCommandOrBatch is DbCommand command)
		{
			static async ValueTask<IDataReader> DoAsync(DbCommand c, CommandBehavior cb, CancellationToken ct) =>
				await c.ExecuteReaderAsync(cb, ct).ConfigureAwait(false);

			return DoAsync(command, commandBehavior, cancellationToken);
		}

#if NET
		if (ActiveCommandOrBatch is DbBatch batch)
		{
			static async ValueTask<IDataReader> DoAsync(DbBatch b, CommandBehavior cb, CancellationToken ct) =>
				await b.ExecuteReaderAsync(cb, ct).ConfigureAwait(false);

			return DoAsync(batch, commandBehavior, cancellationToken);
		}
#endif

		return new ValueTask<IDataReader>(ExecuteReaderCore(commandBehavior));
	}

	/// <summary>
	/// Prepares the active command or batch.
	/// </summary>
	protected virtual void PrepareCore()
	{
		if (ActiveCommandOrBatch is IDbCommand command)
		{
			command.Prepare();
			return;
		}

#if NET
		if (ActiveCommandOrBatch is DbBatch batch)
		{
			batch.Prepare();
			return;
		}
#endif

		throw new NotSupportedException();
	}

	/// <summary>
	/// Prepares the active command or batch asynchronously.
	/// </summary>
	protected virtual ValueTask PrepareCoreAsync(CancellationToken cancellationToken)
	{
#if NET
		if (ActiveCommandOrBatch is DbCommand command)
			return new ValueTask(command.PrepareAsync(cancellationToken));

		if (ActiveCommandOrBatch is DbBatch batch)
			return new ValueTask(batch.PrepareAsync(cancellationToken));
#endif

		PrepareCore();
		return default;
	}

	/// <summary>
	/// Cancels the active command or batch.
	/// </summary>
	protected virtual void CancelCore()
	{
		if (ActiveCommandOrBatch is IDbCommand command)
		{
			command.Cancel();
			return;
		}

#if NET
		if (ActiveCommandOrBatch is DbBatch batch)
		{
			batch.Cancel();
			return;
		}
#endif

		throw new NotSupportedException();
	}

	/// <summary>
	/// Disposes the active command or batch.
	/// </summary>
	protected virtual void DisposeCommandOrBatchCore()
	{
		if (ActiveCommandOrBatch is IDbCommand command)
		{
			command.Dispose();
			return;
		}

#if NET
		if (ActiveCommandOrBatch is DbBatch batch)
		{
			batch.Dispose();
			return;
		}
#endif

		throw new NotSupportedException();
	}

	/// <summary>
	/// Disposes the active command or batch asynchronously.
	/// </summary>
	protected virtual ValueTask DisposeCommandOrBatchCoreAsync()
	{
#if NET
		if (ActiveCommandOrBatch is DbCommand command)
			return command.DisposeAsync();

		if (ActiveCommandOrBatch is DbBatch batch)
			return batch.DisposeAsync();
#endif

		DisposeCommandOrBatchCore();
		return default;
	}

	/// <summary>
	/// Reads the next record from the active reader.
	/// </summary>
	protected virtual bool ReadReaderCore() => ActiveReader!.Read();

	/// <summary>
	/// Reads the next record from the active reader asynchronously.
	/// </summary>
	protected virtual ValueTask<bool> ReadReaderCoreAsync(CancellationToken cancellationToken)
	{
		if (ActiveReader is DbDataReader dbReader)
			return new ValueTask<bool>(dbReader.ReadAsync(cancellationToken));

		return new ValueTask<bool>(ReadReaderCore());
	}

	/// <summary>
	/// Reads the next result from the active reader.
	/// </summary>
	protected virtual bool NextReaderResultCore() => ActiveReader!.NextResult();

	/// <summary>
	/// Reads the next result from the active reader asynchronously.
	/// </summary>
	protected virtual ValueTask<bool> NextReaderResultCoreAsync(CancellationToken cancellationToken)
	{
		if (ActiveReader is DbDataReader dbReader)
			return new ValueTask<bool>(dbReader.NextResultAsync(cancellationToken));

		return new ValueTask<bool>(NextReaderResultCore());
	}

	/// <summary>
	/// Closes the active reader.
	/// </summary>
	protected virtual void CloseReaderCore() => ActiveReader!.Close();

	/// <summary>
	/// Closes the active reader asynchronously.
	/// </summary>
	protected virtual ValueTask CloseReaderCoreAsync()
	{
#if NET
		if (ActiveReader is DbDataReader dbReader)
			return new ValueTask(dbReader.CloseAsync());
#endif

		CloseReaderCore();
		return default;
	}

	/// <summary>
	/// Disposes the active reader.
	/// </summary>
	protected virtual void DisposeReaderCore() => ActiveReader!.Dispose();

	/// <summary>
	/// Disposes the active reader asynchronously.
	/// </summary>
	protected virtual ValueTask DisposeReaderCoreAsync()
	{
#if NET
		if (ActiveReader is DbDataReader dbReader)
			return dbReader.DisposeAsync();
#endif

		DisposeReaderCore();
		return default;
	}

	/// <summary>
	/// Creates a command.
	/// </summary>
	protected virtual IDbCommand CreateCommandCore(CommandType commandType)
	{
		var command = Connection.CreateCommand();
		if (commandType != CommandType.Text)
			command.CommandType = commandType;
		return command;
	}

	/// <summary>
	/// Creates a batch.
	/// </summary>
	protected virtual object CreateBatchCore()
	{
#if NET
		if (Connection is DbConnection dbConnection)
			return dbConnection.CreateBatch();
#endif

		throw new NotSupportedException();
	}

	/// <summary>
	/// Adds a command to the active batch.
	/// </summary>
	protected virtual void AddBatchCommandCore(CommandType commandType)
	{
#if NET
		if (ActiveCommandOrBatch is DbBatch dbBatch)
		{
			var command = dbBatch.CreateBatchCommand();
			if (commandType != CommandType.Text)
				command.CommandType = commandType;
			dbBatch.BatchCommands.Add(command);
			return;
		}
#endif

		throw new NotSupportedException();
	}

	/// <summary>
	/// Sets the timeout of the active command or batch.
	/// </summary>
	protected virtual void SetTimeoutCore(int timeout)
	{
		if (ActiveCommandOrBatch is IDbCommand command)
		{
			command.CommandTimeout = timeout;
			return;
		}

#if NET
		if (ActiveCommandOrBatch is DbBatch dbBatch)
		{
			dbBatch.Timeout = timeout;
			return;
		}
#endif

		throw new NotSupportedException();
	}

	/// <summary>
	/// Sets the transaction of the active command or batch.
	/// </summary>
	protected virtual void SetTransactionCore(IDbTransaction? transaction)
	{
		if (ActiveCommandOrBatch is IDbCommand command)
		{
			command.Transaction = transaction;
			return;
		}

#if NET
		if (ActiveCommandOrBatch is DbBatch dbBatch && transaction is DbTransaction dbTransaction)
		{
			dbBatch.Transaction = dbTransaction;
			return;
		}
#endif

		throw new NotSupportedException();
	}

	/// <summary>
	/// Sets the command text of the specified command.
	/// </summary>
	protected virtual void SetCommandTextCore(int commandIndex, string commandText)
	{
		if (ActiveCommandOrBatch is IDbCommand command && commandIndex == 0)
		{
			command.CommandText = commandText;
			return;
		}

#if NET
		if (ActiveCommandOrBatch is DbBatch dbBatch)
		{
			dbBatch.BatchCommands[commandIndex].CommandText = commandText;
			return;
		}
#endif

		throw new NotSupportedException();
	}

	/// <summary>
	/// Gets the parameter collection of the specified command.
	/// </summary>
	protected virtual IDataParameterCollection GetParameterCollectionCore(int commandIndex)
	{
		if (ActiveCommandOrBatch is IDbCommand command && commandIndex == 0)
			return command.Parameters;

#if NET
		if (ActiveCommandOrBatch is DbBatch dbBatch)
			return dbBatch.BatchCommands[commandIndex].Parameters;
#endif

		throw new NotSupportedException();
	}

	/// <summary>
	/// Creates a parameter with the specified name and value.
	/// </summary>
	protected virtual IDataParameter CreateParameterCore<T>(string name, T value)
	{
		IDataParameter parameter;

		if (ActiveCommandOrBatch is IDbCommand command)
			parameter = command.CreateParameter();
#if NET
		else if (ActiveCommandOrBatch is DbBatch dbBatch)
			parameter = dbBatch.BatchCommands[0].CreateParameter();
#endif
		else
			throw new NotSupportedException();

		if (name.Length != 0)
			parameter.ParameterName = name;
		parameter.Value = value is null ? DBNull.Value : value;
		return parameter;
	}

	/// <summary>
	/// Updates the parameter value of a parameter.
	/// </summary>
	/// <remarks>If the value is a parameter, use its value.</remarks>
	protected virtual void SetParameterValueCore<T>(IDataParameter parameter, T value)
	{
		parameter.Value = value switch
		{
			null => DBNull.Value,
			IDataParameter ddp => ddp.Value,
			_ => value,
		};
	}

	/// <summary>
	/// Raises the <see cref="Executing" /> event.
	/// </summary>
	protected virtual void OnExecuting(DbConnectorCommandBatch commandBatch) =>
		Executing?.Invoke(this, new DbConnectorExecutingEventArgs(commandBatch));

	internal DbDataMapper DataMapper => Settings.DataMapper;

	internal DbConnectorPool? ConnectorPool { get; set; }

	internal int ExecuteCommand(DbConnectorCommandBatch commandBatch)
	{
		OnExecuting(commandBatch);
		using var commandScope = CreateCommand(commandBatch);
		return ExecuteNonQueryCore();
	}

	internal async ValueTask<int> ExecuteCommandAsync(DbConnectorCommandBatch commandBatch, CancellationToken cancellationToken)
	{
		OnExecuting(commandBatch);
		await using var commandScope = (await CreateCommandAsync(commandBatch, cancellationToken).ConfigureAwait(false)).ConfigureAwait(false);
		return await ExecuteNonQueryCoreAsync(cancellationToken).ConfigureAwait(false);
	}

	internal DbResultSetReader QueryMultiple(DbConnectorCommandBatch commandBatch)
	{
		OnExecuting(commandBatch);
		m_hasReadFirstResultSet = false;
		CreateCommand(commandBatch);
		m_activeReader = ExecuteReaderCore();
		return new DbResultSetReader(this);
	}

	internal async ValueTask<DbResultSetReader> QueryMultipleAsync(DbConnectorCommandBatch commandBatch, CancellationToken cancellationToken = default)
	{
		OnExecuting(commandBatch);
		m_hasReadFirstResultSet = false;
		await CreateCommandAsync(commandBatch, cancellationToken).ConfigureAwait(false);
		m_activeReader = await ExecuteReaderCoreAsync(cancellationToken).ConfigureAwait(false);
		return new DbResultSetReader(this);
	}

	internal IReadOnlyList<T> Query<T>(DbConnectorCommandBatch commandBatch, Func<DbConnectorRecord, T>? map)
	{
		OnExecuting(commandBatch);
		using var commandScope = CreateCommand(commandBatch);
		m_activeReader = ExecuteReaderCore();
		using var readerScope = new DbActiveReaderDisposer(this);
		var record = new DbConnectorRecord(this, new DbConnectorRecordState());

		var list = new List<T>();

		do
		{
			while (ReadReaderCore())
				list.Add(map is not null ? map(record) : record.Get<T>());
		}
		while (NextReaderResultCore());

		CloseReaderCore();
		return list;
	}

	internal async ValueTask<IReadOnlyList<T>> QueryAsync<T>(DbConnectorCommandBatch commandBatch, Func<DbConnectorRecord, T>? map, CancellationToken cancellationToken)
	{
		OnExecuting(commandBatch);
		await using var commandScope = (await CreateCommandAsync(commandBatch, cancellationToken).ConfigureAwait(false)).ConfigureAwait(false);
		m_activeReader = await ExecuteReaderCoreAsync(cancellationToken).ConfigureAwait(false);
		await using var readerScope = new DbActiveReaderDisposer(this).ConfigureAwait(false);
		var record = new DbConnectorRecord(this, new DbConnectorRecordState());

		var list = new List<T>();

		do
		{
			while (await ReadReaderCoreAsync(cancellationToken).ConfigureAwait(false))
				list.Add(map is not null ? map(record) : record.Get<T>());
		}
		while (await NextReaderResultCoreAsync(cancellationToken).ConfigureAwait(false));

		await CloseReaderCoreAsync().ConfigureAwait(false);
		return list;
	}

	internal T QueryFirst<T>(DbConnectorCommandBatch commandBatch, Func<DbConnectorRecord, T>? map, bool single, bool orDefault)
	{
		OnExecuting(commandBatch);
		using var commandScope = CreateCommand(commandBatch);
		m_activeReader = single ? ExecuteReaderCore() : ExecuteReaderCore(CommandBehavior.SingleRow);
		using var readerScope = new DbActiveReaderDisposer(this);

		while (!ReadReaderCore())
		{
			if (!NextReaderResultCore())
				return orDefault ? default(T)! : throw new InvalidOperationException("No records were found; use 'OrDefault' to permit this.");
		}

		var record = new DbConnectorRecord(this, state: null);
		var value = map is not null ? map(record) : record.Get<T>();

		if (single && ReadReaderCore())
			throw CreateTooManyRecordsException();

		if (single && NextReaderResultCore())
			throw CreateTooManyRecordsException();

		CloseReaderCore();
		return value;
	}

	internal async ValueTask<T> QueryFirstAsync<T>(DbConnectorCommandBatch commandBatch, Func<DbConnectorRecord, T>? map, bool single, bool orDefault, CancellationToken cancellationToken)
	{
		OnExecuting(commandBatch);
		await using var commandScope = (await CreateCommandAsync(commandBatch, cancellationToken).ConfigureAwait(false)).ConfigureAwait(false);
		m_activeReader = single ? await ExecuteReaderCoreAsync(cancellationToken).ConfigureAwait(false) : await ExecuteReaderCoreAsync(CommandBehavior.SingleRow, cancellationToken).ConfigureAwait(false);
		await using var readerScope = new DbActiveReaderDisposer(this).ConfigureAwait(false);

		while (!await ReadReaderCoreAsync(cancellationToken).ConfigureAwait(false))
		{
			if (!await NextReaderResultCoreAsync(cancellationToken).ConfigureAwait(false))
				return orDefault ? default(T)! : throw CreateNoRecordsException();
		}

		var record = new DbConnectorRecord(this, state: null);
		var value = map is not null ? map(record) : record.Get<T>();

		if (single && await ReadReaderCoreAsync(cancellationToken).ConfigureAwait(false))
			throw CreateTooManyRecordsException();

		if (single && await NextReaderResultCoreAsync(cancellationToken).ConfigureAwait(false))
			throw CreateTooManyRecordsException();

		await CloseReaderCoreAsync().ConfigureAwait(false);
		return value;
	}

	internal IEnumerable<T> Enumerate<T>(DbConnectorCommandBatch commandBatch, Func<DbConnectorRecord, T>? map)
	{
		OnExecuting(commandBatch);
		using var commandScope = CreateCommand(commandBatch);
		m_activeReader = ExecuteReaderCore();
		using var readerScope = new DbActiveReaderDisposer(this);
		var record = new DbConnectorRecord(this, new DbConnectorRecordState());

		do
		{
			while (ReadReaderCore())
				yield return map is not null ? map(record) : record.Get<T>();
		}
		while (NextReaderResultCore());

		CloseReaderCore();
	}

	internal async IAsyncEnumerable<T> EnumerateAsync<T>(DbConnectorCommandBatch commandBatch, Func<DbConnectorRecord, T>? map, [EnumeratorCancellation] CancellationToken cancellationToken)
	{
		OnExecuting(commandBatch);
		await using var commandScope = (await CreateCommandAsync(commandBatch, cancellationToken).ConfigureAwait(false)).ConfigureAwait(false);
		m_activeReader = await ExecuteReaderCoreAsync(cancellationToken).ConfigureAwait(false);
		await using var readerScope = new DbActiveReaderDisposer(this).ConfigureAwait(false);
		var record = new DbConnectorRecord(this, new DbConnectorRecordState());

		do
		{
			while (await ReadReaderCoreAsync(cancellationToken).ConfigureAwait(false))
				yield return map is not null ? map(record) : record.Get<T>();
		}
		while (await NextReaderResultCoreAsync(cancellationToken).ConfigureAwait(false));

		await CloseReaderCoreAsync().ConfigureAwait(false);
	}

	internal List<T> ReadResultSet<T>(Func<DbConnectorRecord, T>? map)
	{
		if (m_hasReadFirstResultSet && !NextReaderResultCore())
			throw CreateNoMoreResultsException();
		m_hasReadFirstResultSet = true;

		var list = new List<T>();
		var record = new DbConnectorRecord(this, new DbConnectorRecordState());
		while (ReadReaderCore())
			list.Add(map is not null ? map(record) : record.Get<T>());
		return list;
	}

	internal async ValueTask<IReadOnlyList<T>> ReadResultSetAsync<T>(Func<DbConnectorRecord, T>? map, CancellationToken cancellationToken)
	{
		if (m_hasReadFirstResultSet && !await NextReaderResultCoreAsync(cancellationToken).ConfigureAwait(false))
			throw CreateNoMoreResultsException();
		m_hasReadFirstResultSet = true;

		var list = new List<T>();
		var record = new DbConnectorRecord(this, new DbConnectorRecordState());
		while (await ReadReaderCoreAsync(cancellationToken).ConfigureAwait(false))
			list.Add(map is not null ? map(record) : record.Get<T>());
		return list;
	}

	internal IEnumerable<T> EnumerateResultSet<T>(Func<DbConnectorRecord, T>? map)
	{
		if (m_hasReadFirstResultSet && !NextReaderResultCore())
			throw CreateNoMoreResultsException();
		m_hasReadFirstResultSet = true;

		var record = new DbConnectorRecord(this, new DbConnectorRecordState());
		while (ReadReaderCore())
			yield return map is not null ? map(record) : record.Get<T>();
	}

	internal async IAsyncEnumerable<T> EnumerateResultSetAsync<T>(Func<DbConnectorRecord, T>? map, [EnumeratorCancellation] CancellationToken cancellationToken)
	{
		if (m_hasReadFirstResultSet && !await NextReaderResultCoreAsync(cancellationToken).ConfigureAwait(false))
			throw CreateNoMoreResultsException();
		m_hasReadFirstResultSet = true;

		var record = new DbConnectorRecord(this, new DbConnectorRecordState());
		while (await ReadReaderCoreAsync(cancellationToken).ConfigureAwait(false))
			yield return map is not null ? map(record) : record.Get<T>();
	}

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

	internal void DisposeActiveCommandOrBatch()
	{
		VerifyNotDisposed();

		if (m_activeCommandOrBatch is not null)
		{
			if (m_activeCommandOrBatchCacheKey is not null)
				CommandCache.AddCommand(m_activeCommandOrBatchCacheKey, m_activeCommandOrBatch);
			else
				DisposeCommandOrBatchCore();

			m_activeCommandOrBatch = null;
			m_activeCommandOrBatchCacheKey = null;
		}
	}

	internal async ValueTask DisposeActiveCommandOrBatchAsync()
	{
		VerifyNotDisposed();

		if (m_activeCommandOrBatch is not null)
		{
			if (m_activeCommandOrBatchCacheKey is not null)
				CommandCache.AddCommand(m_activeCommandOrBatchCacheKey, m_activeCommandOrBatch);
			else
				await DisposeCommandOrBatchCoreAsync().ConfigureAwait(false);

			m_activeCommandOrBatch = null;
			m_activeCommandOrBatchCacheKey = null;
		}
	}

	internal void DisposeActiveReader()
	{
		VerifyNotDisposed();

		if (m_activeReader is not null)
		{
			if (Settings.CancelUnfinishedCommands && !m_activeReader.IsClosed)
				CancelNoThrow();

			DisposeReaderCore();
			m_activeReader = null;
		}
	}

	internal async ValueTask DisposeActiveReaderAsync()
	{
		VerifyNotDisposed();

		if (m_activeReader is not null)
		{
			if (Settings.CancelUnfinishedCommands && !m_activeReader.IsClosed)
				CancelNoThrow();

			await DisposeReaderCoreAsync().ConfigureAwait(false);
			m_activeReader = null;
		}
	}

	private void CancelNoThrow()
	{
		try
		{
			CancelCore();
		}
		catch
		{
			// ignored
		}
	}

	private DbCommandCache CommandCache => m_commandCache ??= new();

	private static InvalidOperationException CreateNoMoreResultsException() => new("No more results.");

	private DbActiveCommandDisposer CreateCommand(DbConnectorCommandBatch commandBatch)
	{
		OpenConnection();

		try
		{
			DoCreateCommand(commandBatch);
			if (ShouldPrepare(commandBatch))
				PrepareCore();
			return new DbActiveCommandDisposer(this);
		}
		catch
		{
			DisposeActiveCommandOrBatch();
			throw;
		}
	}

	private async ValueTask<DbActiveCommandDisposer> CreateCommandAsync(DbConnectorCommandBatch commandBatch, CancellationToken cancellationToken = default)
	{
		await OpenConnectionAsync(cancellationToken).ConfigureAwait(false);

		try
		{
			DoCreateCommand(commandBatch);
			if (ShouldPrepare(commandBatch))
				await PrepareCoreAsync(cancellationToken).ConfigureAwait(false);
			return new DbActiveCommandDisposer(this);
		}
		catch
		{
			await DisposeActiveCommandOrBatchAsync().ConfigureAwait(false);
			throw;
		}
	}

	private bool ShouldPrepare(DbConnectorCommandBatch commandBatch) => commandBatch.IsPrepared ?? Settings.PrepareCommands;

	private bool ShouldCache(DbConnectorCommandBatch commandBatch) => commandBatch.IsCached ?? Settings.CacheCommands;

	private void DoCreateCommand(DbConnectorCommandBatch commandBatch)
	{
		m_activeCommandOrBatch = null;
		m_activeCommandOrBatchCacheKey = null;

		var commandCount = commandBatch.CommandCount;
		var transaction = Transaction;
		var timeout = commandBatch.Timeout ?? Settings.DefaultTimeout;

		var wasCached = false;
		if (ShouldCache(commandBatch))
		{
			if (commandCount == 1)
			{
				var currentCommand = commandBatch.LastCommand;
				var commandText = BuildCommand(currentCommand.TextOrSql, buildText: true);
				m_activeCommandOrBatchCacheKey = commandText;

				m_activeCommandOrBatch = CommandCache.TryRemoveCommand(commandText) as IDbCommand;
				if (m_activeCommandOrBatch is not null)
				{
					wasCached = true;
				}
				else
				{
					m_activeCommandOrBatch = CreateCommandCore(currentCommand.Type);
					SetCommandTextCore(0, commandText);
				}
			}
			else
			{
				var commandTexts = new string[commandCount];
				for (var commandIndex = 0; commandIndex < commandCount; commandIndex++)
					commandTexts[commandIndex] = BuildCommand(commandBatch.GetCommand(commandIndex).TextOrSql, buildText: true);
				m_activeCommandOrBatchCacheKey = commandTexts;

				m_activeCommandOrBatch = CommandCache.TryRemoveCommand(commandTexts);
				if (m_activeCommandOrBatch is not null)
				{
					wasCached = true;
				}
				else
				{
					m_activeCommandOrBatch = CreateBatchCore();
					for (var commandIndex = 0; commandIndex < commandCount; commandIndex++)
					{
						AddBatchCommandCore(commandBatch.GetCommand(commandIndex).Type);
						SetCommandTextCore(commandIndex, commandTexts[commandIndex]);
					}
				}
			}
		}
		else
		{
			if (commandCount == 1)
			{
				var currentCommand = commandBatch.LastCommand;
				m_activeCommandOrBatch = CreateCommandCore(currentCommand.Type);
			}
			else
			{
				m_activeCommandOrBatch = CreateBatchCore();
				for (var commandIndex = 0; commandIndex < commandCount; commandIndex++)
					AddBatchCommandCore(commandBatch.GetCommand(commandIndex).Type);
			}
		}

		if (timeout is not null)
			SetTimeoutCore(timeout == Timeout.InfiniteTimeSpan ? 0 : (int) Math.Ceiling(timeout.Value.TotalSeconds));

		if (transaction is not null || wasCached)
			SetTransactionCore(transaction);

		m_parameterTarget ??= new ParamTarget(this);
		m_parameterTarget.Reset(wasCached);
		for (var commandIndex = 0; commandIndex < commandCount; commandIndex++)
		{
			m_parameterTarget.Parameters = GetParameterCollectionCore(commandIndex);
			var command = commandBatch.GetCommand(commandIndex);

			var commandText = BuildCommand(command.TextOrSql, buildText: !wasCached, m_parameterTarget);
			if (!wasCached)
				SetCommandTextCore(commandIndex, commandText);

			command.Parameters.SubmitParameters(m_parameterTarget);
		}
		m_parameterTarget.Finish();
	}

	private string BuildCommand(object textOrSql, bool buildText, ISqlParamTarget? paramTarget = null)
	{
		if (textOrSql is string text)
			return text;

		if (textOrSql is SqlSource sql)
		{
			var builder = new DbConnectorCommandBuilder(SqlSyntax, buildText, paramTarget);
			sql.Render(builder);
			return builder.GetText();
		}

		throw new InvalidOperationException();
	}

	private void DisposeCachedCommands()
	{
		if (m_commandCache is null)
			return;

		var commands = m_commandCache.GetCommandCollection();
		foreach (var command in commands)
		{
			m_activeCommandOrBatch = command;
			DisposeCommandOrBatchCore();
		}
		m_activeCommandOrBatch = null;
	}

	private ValueTask DisposeCachedCommandsAsync()
	{
		if (m_commandCache is null)
			return default;

		var commands = m_commandCache.GetCommandCollection();
		return commands.Count != 0 ? DoAsync() : default;

		async ValueTask DoAsync()
		{
			foreach (var command in commands)
			{
				m_activeCommandOrBatch = command;
				await DisposeCommandOrBatchCoreAsync().ConfigureAwait(false);
			}
			m_activeCommandOrBatch = null;
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

	private void DisposeDisposables()
	{
		if (m_disposables is not null)
		{
			m_disposables.Reverse();

			foreach (var disposable in m_disposables)
			{
				if (disposable is IDisposable syncDisposable)
					syncDisposable.Dispose();
				else if (disposable is IAsyncDisposable asyncDisposable)
					asyncDisposable.DisposeAsync().AsTask().GetAwaiter().GetResult();
			}

			m_disposables = null;
		}
	}

	private async ValueTask DisposeDisposablesAsync()
	{
		if (m_disposables is not null)
		{
			m_disposables.Reverse();

			foreach (var disposable in m_disposables)
			{
				if (disposable is IAsyncDisposable asyncDisposable)
					await asyncDisposable.DisposeAsync().ConfigureAwait(false);
				else if (disposable is IDisposable syncDisposable)
					syncDisposable.Dispose();
			}

			m_disposables = null;
		}
	}

	private static InvalidOperationException CreateNoRecordsException() => new("No records were found; use 'OrDefault' to permit this.");

	private static InvalidOperationException CreateTooManyRecordsException() => new("Additional records were found; use 'First' to permit this.");

	private sealed class ParamTarget(DbConnector connector) : ISqlParamTarget
	{
		public void Reset(bool wasCached) => m_cachedIndex = wasCached ? 0 : -1;

		public IDataParameterCollection Parameters { get; set; } = null!;

		public void Finish()
		{
			if (m_cachedIndex != -1 && m_cachedIndex < Parameters.Count)
				throw new InvalidOperationException($"Cached commands must always be executed with the same number of parameters (expected {Parameters.Count}, actual {m_cachedIndex}).");
		}

		public void AcceptParameter<T>(string name, T value, SqlParamType? type)
		{
			if (m_cachedIndex == -1)
			{
				if (value is IDataParameter dbParameter)
				{
					if (name.Length != 0)
						dbParameter.ParameterName = name;
				}
				else
				{
					dbParameter = connector.CreateParameterCore(name, value);
				}

				type?.Apply(dbParameter);

				Parameters.Add(dbParameter);
			}
			else
			{
				if (m_cachedIndex >= Parameters.Count)
					throw new InvalidOperationException($"Cached commands must always be executed with the same number of parameters (missing '{name}').");

				var dbParameter = Parameters[m_cachedIndex] as IDataParameter;
				if (dbParameter is null || dbParameter.ParameterName != name)
					throw new InvalidOperationException($"Cached commands must always be executed with the same number of parameters in the same order (found '{dbParameter?.ParameterName}', expected '{name}').");

				connector.SetParameterValueCore(dbParameter, value);

				m_cachedIndex++;
			}
		}

		private int m_cachedIndex;
	}

	private readonly IDbConnection m_connection;
	private IDbTransaction? m_transaction;
	private object? m_activeCommandOrBatch;
	private object? m_activeCommandOrBatchCacheKey;
	private IDataReader? m_activeReader;
	private DbCommandCache? m_commandCache;
	private List<object?>? m_disposables;
	private ParamTarget? m_parameterTarget;
	private readonly bool m_noCloseConnection;
	private bool m_isConnectionOpen;
	private bool m_isDisposed;
	private bool m_noDisposeTransaction;
	private bool m_hasReadFirstResultSet;
}
