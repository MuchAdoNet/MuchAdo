using System.Data;

namespace MuchAdo;

/// <summary>
/// Settings when creating a <see cref="DbConnector"/>.
/// </summary>
public class DbConnectorSettings
{
	/// <summary>
	/// The SQL syntax to use when formatting SQL.
	/// </summary>
	public SqlSyntax SqlSyntax { get; init; } = SqlSyntax.Default;

	/// <summary>
	/// Maps data record values to objects.
	/// </summary>
	public DbDataMapper DataMapper { get; init; } = DbDataMapper.Default;

	/// <summary>
	/// The isolation level used when <c>BeginTransaction(Async)</c> is called without one.
	/// </summary>
	/// <remarks>If not specified, the behavior is provider-specific.</remarks>
	public IsolationLevel? DefaultIsolationLevel { get; init; }

	/// <summary>
	/// The default timeout.
	/// </summary>
	/// <remarks>If not specified, the behavior is provider-specific.</remarks>
	public TimeSpan? DefaultTimeout { get; init; }

	/// <summary>
	/// If true, commands and command batches are cached by default.
	/// </summary>
	public bool CacheCommands { get; init; }

	/// <summary>
	/// If true, commands and command batches are prepared by default.
	/// </summary>
	public bool PrepareCommands { get; init; }

	/// <summary>
	/// If true, does not dispose the connection when the connector is disposed.
	/// </summary>
	public bool NoDisposeConnection { get; init; }

	/// <summary>
	/// If true, the command or batch is cancelled when the active reader is not read to the end.
	/// </summary>
	/// <remarks>This can occur when an exception is thrown or when breaking early out of
	/// an <see cref="DbConnector.Enumerate{T}" /> or <see cref="DbConnector.EnumerateAsync{T}" />
	/// loop.</remarks>
	public bool CancelUnfinishedCommands { get; init; }

	/// <summary>
	/// The retry policy to use when opening database connections.
	/// </summary>
	/// <remarks>If specified, connection opening will be retried according to the policy.</remarks>
	public DbRetryPolicy? RetryOpenPolicy { get; init; }

	internal static DbConnectorSettings Default { get; } = new();
}
