namespace MuchAdo.Sources;

internal abstract class BinaryOperatorSqlSource(IEnumerable<SqlSource> sqls) : SqlSource
{
	public abstract string Lowercase { get; }

	public abstract string Uppercase { get; }

	internal override void Render(DbConnectorCommandBuilder builder)
	{
		var oldTextLength = builder.TextLength;
		SqlSource? firstSql = null;
		DbConnectorBracketScope? outerScope = null;

		foreach (var sql in sqls)
		{
			if (firstSql is null)
			{
				firstSql = sql;
				continue;
			}

			if (outerScope is null)
			{
				outerScope = builder.Bracket("(", ")");
				firstSql.Render(builder);
			}

			using var innerScope = builder.Prefix(builder.TextLength != oldTextLength ? builder.Syntax.LowercaseKeywords ? Lowercase : Uppercase : "");
			sql.Render(builder);
		}

		if (outerScope is { } scope)
			scope.Dispose();
		else if (firstSql is not null)
			firstSql.Render(builder);
	}
}
