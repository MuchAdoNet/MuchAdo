namespace MuchAdo.Sources;

internal sealed class ListSqlSource(IEnumerable<SqlSource> sqls) : JoiningSqlSource(sqls)
{
	public override string Separator => ", ";
}
