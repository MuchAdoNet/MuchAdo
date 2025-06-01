namespace MuchAdo.Sources;

internal abstract class BinaryOperatorSqlSource(IEnumerable<SqlSource> sqls) : SqlSource
{
	public abstract string Lowercase { get; }

	public abstract string Uppercase { get; }

	internal override void Render(DbConnectorCommandBuilder builder)
	{
		var oldTextLength = builder.TextLength;
		SqlSource? firstSql = null;
		var firstSqlRendered = false;

		foreach (var sql in sqls)
		{
			if (firstSql is null)
			{
				firstSql = sql;
				continue;
			}

			if (!firstSqlRendered)
			{
				using var firstSqlScope = builder.Bracket("(", ")");
				firstSql.Render(builder);
				firstSqlRendered = true;
			}

			using var opScope = builder.Prefix(builder.TextLength == oldTextLength ? "" : builder.Syntax.LowercaseKeywords ? Lowercase : Uppercase);
			using var sqlScope = builder.Bracket("(", ")");
			sql.Render(builder);
		}

		if (firstSql is not null && !firstSqlRendered)
			firstSql.Render(builder);
	}
}
