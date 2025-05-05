namespace MuchAdo.Sources;

internal sealed class ClausesSqlSource(IEnumerable<SqlSource> sqls) : JoiningSqlSource(sqls)
{
	public override string Separator => "\n";
}
