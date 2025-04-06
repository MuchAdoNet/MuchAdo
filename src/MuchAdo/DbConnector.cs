using System.Data;
using MuchAdo.SqlFormatting;

namespace MuchAdo;

/// <summary>
/// Encapsulates a database connection and any current transaction.
/// </summary>
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
		ProviderMethods = settings.ProviderMethods ?? DbProviderMethods.Default;
		m_defaultIsolationLevel = settings.DefaultIsolationLevel;
		SqlSyntax = settings.SqlSyntax ?? SqlSyntax.Default;
		DataMapper = settings.DataMapper ?? DbDataMapper.Default;
	}

	/// <summary>
	/// The database connection.
	/// </summary>
	/// <remarks>Use <see cref="GetOpenConnectionAsync" /> or <see cref="GetOpenConnection" />
	/// to automatically open the connection if necessary.</remarks>
	public IDbConnection Connection => m_connection;

	/// <summary>
	/// The current transaction, if any.
	/// </summary>
	public IDbTransaction? CurrentTransaction => m_transaction;

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

		m_connection.Open();
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
			await ProviderMethods.OpenConnectionAsync(m_connection, cancellationToken).ConfigureAwait(false);
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

		m_connection.Open();
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
			await ProviderMethods.OpenConnectionAsync(m_connection, cancellationToken).ConfigureAwait(false);
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
		m_transaction = m_defaultIsolationLevel is { } isolationLevel
			? GetOpenConnection().BeginTransaction(isolationLevel)
			: GetOpenConnection().BeginTransaction();
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
		m_transaction = GetOpenConnection().BeginTransaction(isolationLevel);
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
		var connection = await GetOpenConnectionAsync(cancellationToken).ConfigureAwait(false);
		m_transaction = m_defaultIsolationLevel is { } isolationLevel
			? await ProviderMethods.BeginTransactionAsync(connection, isolationLevel, cancellationToken).ConfigureAwait(false)
			: await ProviderMethods.BeginTransactionAsync(connection, cancellationToken).ConfigureAwait(false);
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
		var connection = await GetOpenConnectionAsync(cancellationToken).ConfigureAwait(false);
		m_transaction = await ProviderMethods.BeginTransactionAsync(connection, isolationLevel, cancellationToken).ConfigureAwait(false);
		return new DbTransactionDisposer(this);
	}

	/// <summary>
	/// Attaches a transaction.
	/// </summary>
	/// <returns>An <see cref="IDisposable" /> that should be disposed when the transaction has been committed or should be rolled back.</returns>
	public DbTransactionDisposer AttachTransaction(IDbTransaction transaction, bool noDispose = false)
	{
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
		VerifyGetTransaction().Commit();
		DisposeTransaction();
	}

	/// <summary>
	/// Commits the current transaction.
	/// </summary>
	/// <param name="cancellationToken">The cancellation token.</param>
	/// <seealso cref="CommitTransaction" />
	public async ValueTask CommitTransactionAsync(CancellationToken cancellationToken = default)
	{
		await ProviderMethods.CommitTransactionAsync(VerifyGetTransaction(), cancellationToken).ConfigureAwait(false);
		await DisposeTransactionAsync().ConfigureAwait(false);
	}

	/// <summary>
	/// Rolls back the current transaction.
	/// </summary>
	/// <seealso cref="RollbackTransactionAsync" />
	public void RollbackTransaction()
	{
		VerifyGetTransaction().Rollback();
		DisposeTransaction();
	}

	/// <summary>
	/// Rolls back the current transaction.
	/// </summary>
	/// <param name="cancellationToken">The cancellation token.</param>
	/// <seealso cref="RollbackTransaction" />
	public async ValueTask RollbackTransactionAsync(CancellationToken cancellationToken = default)
	{
		await ProviderMethods.RollbackTransactionAsync(VerifyGetTransaction(), cancellationToken).ConfigureAwait(false);
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

		m_connection.Close();
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
			await ProviderMethods.CloseConnectionAsync(m_connection).ConfigureAwait(false);
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
		m_disposable.Dispose();
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
				await ProviderMethods.DisposeConnectionAsync(m_connection).ConfigureAwait(false);
			await m_disposable.DisposeAsync().ConfigureAwait(false);
			m_isDisposed = true;
		}
	}

	internal DbDataMapper DataMapper { get; }

	internal DbProviderMethods ProviderMethods { get; }

	internal DbCommandCache CommandCache => m_commandCache ??= new();

	internal DbConnectorPool? ConnectorPool { get; set; }

	internal void DisposeTransaction()
	{
		VerifyNotDisposed();

		var transaction = m_transaction;
		m_transaction = null;

		if (!m_noDisposeTransaction && transaction is not null)
			transaction.Dispose();
	}

	internal ValueTask DisposeTransactionAsync()
	{
		VerifyNotDisposed();

		var transaction = m_transaction;
		m_transaction = null;

		return !m_noDisposeTransaction && transaction is not null ? DoAsync() : default;

		async ValueTask DoAsync() => await ProviderMethods.DisposeTransactionAsync(transaction).ConfigureAwait(false);
	}

	private void DisposeCachedCommands()
	{
		if (m_commandCache is null)
			return;

		var commands = m_commandCache.GetCommands();
		foreach (var command in commands)
			CachedCommand.Unwrap(command).Dispose();
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
				await ProviderMethods.DisposeCommandAsync(CachedCommand.Unwrap(command)).ConfigureAwait(false);
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

		if (m_transaction is not null)
			throw new InvalidOperationException("A transaction is already started.");
	}

	private IDbTransaction VerifyGetTransaction()
	{
		VerifyNotDisposed();

		if (m_transaction is null)
			throw new InvalidOperationException("No transaction available; call BeginTransaction first.");

		return m_transaction;
	}

	private static readonly DbConnectorSettings s_defaultSettings = new();

	private readonly bool m_noDisposeConnection;
	private readonly bool m_noCloseConnection;
	private readonly IsolationLevel? m_defaultIsolationLevel;
	private readonly IDbConnection m_connection;
	private IDbTransaction? m_transaction;
	private DbCommandCache? m_commandCache;
	private AsyncScope m_disposable;
	private bool m_isConnectionOpen;
	private bool m_isDisposed;
	private bool m_noDisposeTransaction;
}
