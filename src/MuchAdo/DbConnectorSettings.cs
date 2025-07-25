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
	/// The transaction settings used when not specified.
	/// </summary>
	/// <remarks>If not specified, <c>DbTransactionSettings.Default</c> is used.</remarks>
	public DbTransactionSettings? DefaultTransactionSettings { get; init; }

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
	/// The retry policy to use when opening a database connection or calling a <c>Retry</c> method.
	/// </summary>
	/// <remarks><para>When retry requests are nested, only the outermost action is retried.</para>
	/// <para>If not set, opening a database connection will not be retried, and <c>Retry</c> methods
	/// will throw an exception.</para></remarks>
	public DbRetryPolicy? RetryPolicy { get; init; }

	internal static DbConnectorSettings Default { get; } = new();
}
