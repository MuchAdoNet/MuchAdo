using MuchAdo.SqlFormatting;

namespace MuchAdo.MySql;

public class MySqlDbConnectorSettings : DbConnectorSettings
{
	public MySqlDbConnectorSettings()
	{
		SqlSyntax = SqlSyntax.MySql;
	}
}
