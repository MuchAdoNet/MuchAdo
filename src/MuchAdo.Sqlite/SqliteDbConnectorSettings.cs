namespace MuchAdo.Sqlite;

/// <summary>
/// Settings when creating a <see cref="SqliteDbConnector" />.
/// </summary>
public class SqliteDbConnectorSettings : DbConnectorSettings
{
	public SqliteDbConnectorSettings()
	{
		SqlSyntax = SqlSyntax.Sqlite;
	}

	internal static SqliteDbConnectorSettings Default { get; } = new();
}
