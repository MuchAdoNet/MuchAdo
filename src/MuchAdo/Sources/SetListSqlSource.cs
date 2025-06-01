namespace MuchAdo.Sources;

internal sealed class SetListSqlSource(IEnumerable<SqlSource> sqls) : InterspersingSqlSource(sqls)
{
	public override string Separator => ", ";

	public override string TextOnEmpty => throw new InvalidOperationException("Set must not be empty.");
}
