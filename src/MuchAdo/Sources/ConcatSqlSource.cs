namespace MuchAdo.Sources;

internal sealed class ConcatSqlSource(IEnumerable<SqlSource> sqls) : SqlSource
{
	internal override void Render(DbConnectorCommandBuilder builder)
	{
		foreach (var sql in sqls)
			sql.Render(builder);
	}
}
