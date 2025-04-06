using System.Runtime.CompilerServices;

namespace MuchAdo.SqlFormatting;

[InterpolatedStringHandler]
public readonly ref struct SqlFormatStringHandler
{
	public SqlFormatStringHandler(int literalLength, int formattedCount)
	{
		m_sqls = new(capacity: formattedCount * 2 + 1);
	}

	public void AppendLiteral(string s) => m_sqls.Add(Sql.Raw(s));

	public void AppendFormatted<T>(T t) => m_sqls.Add(t as Sql ?? Sql.Param(t));

	internal Sql ToSql() => Sql.Concat(m_sqls);

	private readonly List<Sql> m_sqls;
}
