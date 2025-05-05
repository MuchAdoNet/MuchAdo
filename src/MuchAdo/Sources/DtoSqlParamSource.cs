namespace MuchAdo.Sources;

public sealed class DtoSqlParamSource<T> : SqlParamSource
{
	public new DtoSqlParamSource<T> Where(Func<string, bool> filterName) =>
		new(m_dto, m_filterName is null ? filterName : x => m_filterName(x) && filterName(x));

	internal DtoSqlParamSource(T dto, Func<string, bool>? filterName = null)
	{
		m_dto = dto;
		m_filterName = filterName;
	}

	internal override void SubmitParameters(ISqlParamTarget target)
	{
		var properties = DbDtoInfo.GetInfo<T>().Properties;
		if (properties.Count == 0)
			throw new InvalidOperationException($"The specified type has no columns: {typeof(T).FullName}");

		foreach (var property in properties)
		{
			if (m_filterName is null || m_filterName(property.Name))
				property.SubmitParameter(target, "", m_dto, type: null);
		}
	}

	private readonly T m_dto;
	private readonly Func<string, bool>? m_filterName;
}
