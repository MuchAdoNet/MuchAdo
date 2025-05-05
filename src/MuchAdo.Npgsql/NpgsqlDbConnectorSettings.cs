namespace MuchAdo.Npgsql;

/// <summary>
/// Settings when creating a <see cref="NpgsqlDbConnector" />.
/// </summary>
public class NpgsqlDbConnectorSettings : DbConnectorSettings
{
	public NpgsqlDbConnectorSettings()
	{
		SqlSyntax = SqlSyntax.Postgres;
	}

	internal static NpgsqlDbConnectorSettings Default { get; } = new();
}
