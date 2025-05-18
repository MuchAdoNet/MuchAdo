namespace MuchAdo.Sources;

internal sealed class RepeatTypedSqlParam<T>(T value, SqlParamType type) : TypedSqlParam<T>(value, type)
{
	internal override bool IsRepeatable => true;
}
