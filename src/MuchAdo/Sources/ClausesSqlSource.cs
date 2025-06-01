namespace MuchAdo.Sources;

internal sealed class ClausesSqlSource(IEnumerable<SqlSource> sqls) : InterspersingSqlSource(sqls)
{
	public override string Separator => "\n";
}
