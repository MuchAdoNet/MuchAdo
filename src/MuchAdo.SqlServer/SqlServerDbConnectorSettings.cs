namespace MuchAdo.SqlServer;

/// <summary>
/// Settings when creating a <see cref="SqlServerDbConnector" />.
/// </summary>
public class SqlServerDbConnectorSettings : DbConnectorSettings
{
	public SqlServerDbConnectorSettings()
	{
		SqlSyntax = SqlSyntax.SqlServer;
	}

	internal static SqlServerDbConnectorSettings Default { get; } = new();
}
