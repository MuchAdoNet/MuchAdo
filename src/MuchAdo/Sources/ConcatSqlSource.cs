namespace MuchAdo.Sources;

internal sealed class ConcatSqlSource(IEnumerable<SqlSource> sqls) : SqlSource
{
	private readonly IEnumerable<SqlSource> m_sqls = sqls.Memoize();

	internal override void Render(DbConnectorCommandBuilder builder)
	{
		foreach (var sql in m_sqls)
			sql.Render(builder);
	}
}
