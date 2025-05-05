namespace MuchAdo.Sources;

internal abstract class OptionalClauseSqlSource(SqlSource sql) : SqlSource
{
	public abstract string Lowercase { get; }

	public abstract string Uppercase { get; }

	internal override void Render(DbConnectorCommandBuilder builder)
	{
		using var scope = builder.Prefix(builder.Syntax.LowercaseKeywords ? Lowercase : Uppercase);
		sql.Render(builder);
	}
}
