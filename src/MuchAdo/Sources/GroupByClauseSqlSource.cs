namespace MuchAdo.Sources;

internal sealed class GroupByClauseSqlSource(SqlSource sql) : OptionalClauseSqlSource(sql)
{
	public override string Lowercase => "group by ";

	public override string Uppercase => "GROUP BY ";
}
