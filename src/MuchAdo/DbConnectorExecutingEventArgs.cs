namespace MuchAdo;

/// <summary>
/// Raised before a command batch is executed.
/// </summary>
public sealed class DbConnectorExecutingEventArgs(DbConnectorCommandBatch commandBatch) : EventArgs
{
	/// <summary>
	/// The command batch that is about to be executed.
	/// </summary>
	public DbConnectorCommandBatch CommandBatch { get; } = commandBatch;
}
