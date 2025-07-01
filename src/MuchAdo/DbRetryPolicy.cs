namespace MuchAdo;

/// <summary>
/// A retry policy for database operations.
/// </summary>
public abstract class DbRetryPolicy
{
	/// <summary>
	/// Executes the specified action with retry logic.
	/// </summary>
	public abstract void Execute(DbConnector connector, Action action);

	/// <summary>
	/// Executes the specified asynchronous action with retry logic.
	/// </summary>
	public abstract ValueTask ExecuteAsync(DbConnector connector, Func<CancellationToken, ValueTask> action, CancellationToken cancellationToken = default);
}
