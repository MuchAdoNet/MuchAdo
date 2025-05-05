namespace MuchAdo;

internal interface ISqlParamTarget
{
	void AcceptParameter<T>(string name, T value, SqlParamType? type);
}
