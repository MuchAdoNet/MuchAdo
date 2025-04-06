namespace MuchAdo;

/// <summary>
/// Closes the connection when disposed.
/// </summary>
public readonly struct DbConnectionCloser(DbConnector? connector) : IDisposable, IAsyncDisposable
{
	/// <summary>
	/// Closes the connection.
	/// </summary>
	public void Dispose() => connector?.CloseConnection();

	/// <summary>
	/// Closes the connection.
	/// </summary>
	public ValueTask DisposeAsync() => connector?.CloseConnectionAsync() ?? default;
}
