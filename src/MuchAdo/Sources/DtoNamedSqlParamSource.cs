namespace MuchAdo.Sources;

internal sealed class DtoNamedSqlParamSource<T>(T dto) : SqlParamSource
{
	internal override void SubmitParameters(ISqlParamTarget target)
	{
		foreach (var property in DbDtoInfo.GetInfo<T>().Properties)
			property.SubmitParameter(target, property.Name, dto, type: null);
	}
}
