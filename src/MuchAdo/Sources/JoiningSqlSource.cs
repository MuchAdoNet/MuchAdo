namespace MuchAdo.Sources;

internal abstract class JoiningSqlSource(IEnumerable<SqlSource> sqls) : SqlSource
{
	public abstract string Separator { get; }

	public virtual string TextOnEmpty => "";

	internal override void Render(DbConnectorCommandBuilder builder)
	{
		var oldTextLength = builder.TextLength;

		foreach (var sql in sqls)
		{
			using var scope = builder.Prefix(builder.TextLength != oldTextLength ? Separator : "");
			sql.Render(builder);
		}

		if (builder.TextLength == oldTextLength)
			builder.AppendText(TextOnEmpty);
	}
}
