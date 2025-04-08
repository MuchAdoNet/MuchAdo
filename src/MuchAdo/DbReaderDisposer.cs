namespace MuchAdo;

internal readonly struct DbReaderDisposer(DbConnector? connector) : IDisposable, IAsyncDisposable
{
	public void Dispose() => connector?.DisposeActiveReader();

	public ValueTask DisposeAsync() => connector?.DisposeActiveReaderAsync() ?? default;
}
