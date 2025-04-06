namespace MuchAdo;

/// <summary>
/// Maintains a pool of connectors.
/// </summary>
/// <remarks>This is potentially useful if your ADO.NET provider doesn't do its own connection pooling.
/// The database connection of a pooled connector will retain whatever state it had when it was returned
/// to the pool.</remarks>
public sealed class DbConnectorPool : IDisposable, IAsyncDisposable
{
	/// <summary>
	/// Creates a new connector pool.
	/// </summary>
	/// <param name="settings">The settings.</param>
	public DbConnectorPool(DbConnectorPoolSettings settings)
	{
		_ = settings ?? throw new ArgumentNullException(nameof(settings));
		m_create = settings.CreateConnector ?? throw new ArgumentException($"{nameof(settings.CreateConnector)} is required.");

		m_lock = new();
		m_idleConnectors = new Stack<DbConnector>();
	}

	/// <summary>
	/// Returns a connector from the pool or creates a new connector if the pool is empty.
	/// </summary>
	/// <remarks>Dispose the returned connector to return it to the pool.</remarks>
	public DbConnector Get()
	{
		DbConnector? connector = null;

		lock (m_lock)
		{
			if (m_idleConnectors is null)
				throw new ObjectDisposedException(nameof(DbConnectorPool));
			if (m_idleConnectors.Count != 0)
				connector = m_idleConnectors.Pop();
		}

		connector ??= m_create();
		connector.ConnectorPool = this;
		return connector;
	}

	/// <summary>
	/// Disposes the connector pool.
	/// </summary>
	/// <seealso cref="DisposeAsync" />
	public void Dispose()
	{
		Stack<DbConnector>? connectors;

		lock (m_lock)
		{
			connectors = m_idleConnectors;
			m_idleConnectors = null;
		}

		if (connectors is not null)
		{
			foreach (var connector in connectors)
				connector.Dispose();
		}
	}

	/// <summary>
	/// Disposes the connector pool.
	/// </summary>
	/// <seealso cref="Dispose" />
	public async ValueTask DisposeAsync()
	{
		Stack<DbConnector>? connectors;

		lock (m_lock)
		{
			connectors = m_idleConnectors;
			m_idleConnectors = null;
		}

		if (connectors is not null)
		{
			foreach (var connector in connectors)
				await connector.DisposeAsync().ConfigureAwait(false);
		}
	}

	internal void ReturnConnector(DbConnector connector)
	{
		lock (m_lock)
		{
			if (m_idleConnectors is null)
				throw new InvalidOperationException($"{nameof(DbConnectorPool)} was disposed.");
			m_idleConnectors.Push(connector);
			connector.ConnectorPool = null;
		}
	}

	private readonly Func<DbConnector> m_create;
#if NET9_0_OR_GREATER
	private readonly Lock m_lock;
#else
	private readonly object m_lock;
#endif
	private Stack<DbConnector>? m_idleConnectors;
}
