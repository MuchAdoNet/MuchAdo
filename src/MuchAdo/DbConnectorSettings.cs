using System.Data;
using MuchAdo.SqlFormatting;

namespace MuchAdo;

/// <summary>
/// Settings when creating a <see cref="DbConnector"/>.
/// </summary>
public class DbConnectorSettings
{
	/// <summary>
	/// If true, does not dispose the connection when the connector is disposed.
	/// </summary>
	public bool NoDispose { get; set; }

	/// <summary>
	/// Provider-specific database methods.
	/// </summary>
	public DbProviderMethods? ProviderMethods { get; set; }

	/// <summary>
	/// The SQL syntax to use when formatting SQL.
	/// </summary>
	public SqlSyntax? SqlSyntax { get; set; }

	/// <summary>
	/// The isolation level used when <c>BeginTransaction(Async)</c> is called without one.
	/// </summary>
	/// <remarks>If not specified, the behavior is provider-specific.</remarks>
	public IsolationLevel? DefaultIsolationLevel { get; set; }

	/// <summary>
	/// Maps data record values to objects.
	/// </summary>
	public DbDataMapper? DataMapper { get; set; }
}
