namespace MuchAdo.Sources;

internal sealed class WhereClauseSqlSource(SqlSource sql) : OptionalClauseSqlSource(sql)
{
	public override string Lowercase => "where ";

	public override string Uppercase => "WHERE ";
}
