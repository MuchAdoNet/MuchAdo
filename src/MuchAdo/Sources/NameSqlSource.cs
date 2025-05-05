namespace MuchAdo.Sources;

internal sealed class NameSqlSource(string identifier) : SqlSource
{
	internal override void Render(DbConnectorCommandBuilder builder)
	{
		var (start, escaped, end) = builder.Syntax.QuoteName(identifier);
		builder.AppendText(start);
		builder.AppendText(escaped);
		builder.AppendText(end);
	}
}
