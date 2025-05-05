namespace MuchAdo.Sources;

internal sealed class NamedTypedSqlParam<T>(string name, T value, SqlParamType type) : SqlParam<T>(value)
{
	public override string Name => name;

	public override SqlParamType? Type => type;
}
