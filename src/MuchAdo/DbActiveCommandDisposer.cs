namespace MuchAdo;

internal readonly struct DbActiveCommandDisposer(DbConnector? connector) : IDisposable, IAsyncDisposable
{
	public void Dispose() => connector?.DisposeActiveCommandOrBatch();

	public ValueTask DisposeAsync() => connector?.DisposeActiveCommandOrBatchAsync() ?? default;
}
