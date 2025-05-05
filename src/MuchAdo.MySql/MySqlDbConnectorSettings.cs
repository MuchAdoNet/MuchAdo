namespace MuchAdo.MySql;

/// <summary>
/// Settings when creating a <see cref="MySqlDbConnector" />.
/// </summary>
public class MySqlDbConnectorSettings : DbConnectorSettings
{
	public MySqlDbConnectorSettings()
	{
		SqlSyntax = SqlSyntax.MySql;
		DataMapper = MySqlDbDataMapper.Default;
	}

	internal static MySqlDbConnectorSettings Default { get; } = new();
}
