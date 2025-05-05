namespace MuchAdo.Sources;

internal sealed class ReusedTypedSqlParam<T>(T value, SqlParamType type) : TypedSqlParam<T>(value, type)
{
	internal override bool IsReused => true;
}
