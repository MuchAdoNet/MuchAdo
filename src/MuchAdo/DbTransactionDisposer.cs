namespace MuchAdo;

/// <summary>
/// Disposes the current transaction when disposed.
/// </summary>
public readonly struct DbTransactionDisposer(DbConnector? connector) : IDisposable, IAsyncDisposable
{
	/// <summary>
	/// Disposes the transaction.
	/// </summary>
	public void Dispose() => connector?.DisposeTransaction();

	/// <summary>
	/// Disposes the transaction.
	/// </summary>
	public ValueTask DisposeAsync() => connector?.DisposeTransactionAsync() ?? default;
}
