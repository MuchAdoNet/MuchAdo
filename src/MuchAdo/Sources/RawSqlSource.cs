namespace MuchAdo.Sources;

internal sealed class RawSqlSource(string text) : SqlSource
{
	internal override void Render(DbConnectorCommandBuilder builder) => builder.AppendText(text);
}
