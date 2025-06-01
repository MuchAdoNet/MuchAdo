namespace MuchAdo.Sources;

internal sealed class IntersperseSqlSource(string separator, IEnumerable<SqlSource> sqls) : InterspersingSqlSource(sqls)
{
	public override string Separator => separator;
}
