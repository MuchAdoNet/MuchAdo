namespace MuchAdo.Sources;

internal sealed class FormatParamSqlSource<T>(T value) : SqlSource
{
	internal override void Render(DbConnectorCommandBuilder builder) =>
		builder.AppendParameterValue(value);
}
