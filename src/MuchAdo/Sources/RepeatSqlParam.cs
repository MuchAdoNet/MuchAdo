namespace MuchAdo.Sources;

internal sealed class RepeatSqlParam<T>(T value) : SqlParam<T>(value)
{
	internal override bool IsRepeatable => true;
}
