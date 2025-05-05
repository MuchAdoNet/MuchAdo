namespace MuchAdo.Sources;

internal sealed class AndOperatorSqlSource(IEnumerable<SqlSource> sqls) : BinaryOperatorSqlSource(sqls)
{
	public override string Lowercase => " and ";

	public override string Uppercase => " AND ";
}
