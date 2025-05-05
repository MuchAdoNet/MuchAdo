namespace MuchAdo.Sources;

internal sealed class FormatSqlSource(List<object> parts) : SqlSource
{
	internal override void Render(DbConnectorCommandBuilder builder)
	{
		foreach (var part in parts)
		{
			if (part is string text)
				builder.AppendText(text);
			else
				((SqlSource) part).Render(builder);
		}
	}
}
