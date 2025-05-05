namespace MuchAdo.Sources;

internal sealed class OrderByClauseSqlSource(SqlSource sql) : OptionalClauseSqlSource(sql)
{
	public override string Lowercase => "order by ";

	public override string Uppercase => "ORDER BY ";
}
