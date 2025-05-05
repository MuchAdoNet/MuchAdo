namespace MuchAdo;

internal readonly struct DbActiveReaderDisposer(DbConnector? connector) : IDisposable, IAsyncDisposable
{
	public void Dispose() => connector?.DisposeActiveReader();

	public ValueTask DisposeAsync() => connector?.DisposeActiveReaderAsync() ?? default;
}
