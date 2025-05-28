namespace MuchAdo.Sources;

public sealed class DtoSqlParamSource<T> : SqlParamSource
{
	public new DtoSqlParamSource<T> Where(Func<string, bool> nameMatches) =>
		new(m_dto, m_nameMatches is null ? nameMatches : x => m_nameMatches(x) && nameMatches(x));

	internal DtoSqlParamSource(T dto, Func<string, bool>? nameMatches = null)
	{
		m_dto = dto;
		m_nameMatches = nameMatches;
	}

	internal override void SubmitParameters(ISqlParamTarget target)
	{
		var properties = DbDtoInfo.GetInfo<T>().Properties;
		if (properties.Count == 0)
			throw new InvalidOperationException($"The specified type has no columns: {typeof(T).FullName}");

		foreach (var property in properties)
		{
			if (m_nameMatches is null || m_nameMatches(property.Name))
				property.SubmitParameter(target, "", m_dto, type: null);
		}
	}

	private readonly T m_dto;
	private readonly Func<string, bool>? m_nameMatches;
}
