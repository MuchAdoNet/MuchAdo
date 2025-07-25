namespace MuchAdo;

/// <summary>
/// A retry policy for database operations.
/// </summary>
public abstract class DbRetryPolicy
{
	/// <summary>
	/// Executes the specified action with retry logic.
	/// </summary>
	protected abstract void ExecuteCore(DbConnector connector, Action action);

	/// <summary>
	/// Executes the specified asynchronous action with retry logic.
	/// </summary>
	protected abstract ValueTask ExecuteCoreAsync(DbConnector connector, Func<CancellationToken, ValueTask> action, CancellationToken cancellationToken = default);

	internal void Execute(DbConnector connector, Action action)
	{
		if (connector.IsRetrying)
		{
			action();
		}
		else
		{
			try
			{
				connector.IsRetrying = true;
				ExecuteCore(connector, action);
			}
			finally
			{
				connector.IsRetrying = false;
			}
		}
	}

	internal T Execute<T>(DbConnector connector, Func<T> action)
	{
		T result = default!;
		Execute(connector, () =>
		{
			result = action();
		});
		return result;
	}

	internal async ValueTask ExecuteAsync(DbConnector connector, Func<CancellationToken, ValueTask> action, CancellationToken cancellationToken = default)
	{
		if (connector.IsRetrying)
		{
			await action(cancellationToken).ConfigureAwait(false);
		}
		else
		{
			try
			{
				connector.IsRetrying = true;
				await ExecuteCoreAsync(connector, action, cancellationToken).ConfigureAwait(false);
			}
			finally
			{
				connector.IsRetrying = false;
			}
		}
	}

	internal async ValueTask<T> ExecuteAsync<T>(DbConnector connector, Func<CancellationToken, ValueTask<T>> action, CancellationToken cancellationToken = default)
	{
		T result = default!;
		await ExecuteAsync(connector, async ct =>
		{
			result = await action(ct).ConfigureAwait(false);
		}, cancellationToken).ConfigureAwait(false);
		return result;
	}
}
