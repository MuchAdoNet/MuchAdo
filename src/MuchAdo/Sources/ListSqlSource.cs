namespace MuchAdo.Sources;

internal sealed class ListSqlSource(IEnumerable<SqlSource> sqls) : InterspersingSqlSource(sqls)
{
	public override string Separator => ", ";
}
