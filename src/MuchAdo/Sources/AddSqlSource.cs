namespace MuchAdo.Sources;

internal sealed class AddSqlSource(SqlSource a, SqlSource b) : SqlSource
{
	internal override void Render(DbConnectorCommandBuilder builder)
	{
		a.Render(builder);
		b.Render(builder);
	}
}
