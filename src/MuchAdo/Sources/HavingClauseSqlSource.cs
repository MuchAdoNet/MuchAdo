namespace MuchAdo.Sources;

internal sealed class HavingClauseSqlSource(SqlSource sql) : OptionalClauseSqlSource(sql)
{
	public override string Lowercase => "having ";

	public override string Uppercase => "HAVING ";
}
