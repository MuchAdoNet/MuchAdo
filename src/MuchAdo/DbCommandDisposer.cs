namespace MuchAdo;

internal readonly struct DbCommandDisposer(DbConnector? connector) : IDisposable, IAsyncDisposable
{
	public void Dispose() => connector?.DisposeActiveCommand();

	public ValueTask DisposeAsync() => connector?.DisposeActiveCommandAsync() ?? default;
}
