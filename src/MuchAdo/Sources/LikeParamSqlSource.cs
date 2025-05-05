namespace MuchAdo.Sources;

internal sealed class LikeParamSqlSource(Func<Func<string, string>, string> escaper) : SqlSource
{
	internal override void Render(DbConnectorCommandBuilder builder) =>
		builder.AppendParameterValue(escaper(builder.Syntax.EscapeLikeFragment));
}
