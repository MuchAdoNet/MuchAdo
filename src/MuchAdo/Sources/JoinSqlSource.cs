namespace MuchAdo.Sources;

internal sealed class JoinSqlSource(string separator, IEnumerable<SqlSource> sqls) : JoiningSqlSource(sqls)
{
	public override string Separator => separator;
}
