namespace MuchAdo;

internal readonly struct AsyncScope : IAsyncDisposable, IDisposable
{
	public AsyncScope(object disposable) => m_disposable = disposable;

	public bool IsDefault => m_disposable is null;

	public ValueTask DisposeAsync()
	{
		if (m_disposable is IAsyncDisposable asyncDisposable)
			return asyncDisposable.DisposeAsync();

		if (m_disposable is IDisposable disposable)
			disposable.Dispose();

		return default;
	}

	public void Dispose()
	{
		if (m_disposable is IDisposable disposable)
			disposable.Dispose();
		else if (m_disposable is IAsyncDisposable asyncDisposable)
			asyncDisposable.DisposeAsync().AsTask().GetAwaiter().GetResult();
	}

	private readonly object? m_disposable;
}
