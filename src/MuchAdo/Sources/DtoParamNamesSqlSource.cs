namespace MuchAdo.Sources;

public sealed class DtoParamNamesSqlSource<T> : SqlSource
{
	public DtoParamNamesSqlSource<T> Where(Func<string, bool> nameMatches)
	{
		if (m_filterName is not null && m_transformName is not null)
			return new(filterName: x => m_filterName(x) && nameMatches(m_transformName(x)), transformName: m_transformName);
		if (m_filterName is not null)
			return new(filterName: x => m_filterName(x) && nameMatches(x));
		if (m_transformName is not null)
			return new(filterName: x => nameMatches(m_transformName(x)), transformName: m_transformName);
		return new(filterName: nameMatches);
	}

	public DtoParamNamesSqlSource<T> Renamed(Func<string, string> transform)
	{
		if (m_filterName is not null && m_transformName is not null)
			return new(filterName: m_filterName, transformName: x => transform(m_transformName(x)));
		if (m_filterName is not null)
			return new(filterName: m_filterName, transformName: transform);
		if (m_transformName is not null)
			return new(transformName: x => transform(m_transformName(x)));
		return new(transformName: transform);
	}

	internal DtoParamNamesSqlSource(Func<string, bool>? filterName = null, Func<string, string>? transformName = null)
	{
		m_filterName = filterName;
		m_transformName = transformName;
	}

	internal override void Render(DbConnectorCommandBuilder builder)
	{
		var properties = DbDtoInfo.GetInfo<T>().Properties;
		if (properties.Count == 0)
			throw new InvalidOperationException($"The specified type has no columns: {typeof(T).FullName}");

		var oldTextLength = builder.TextLength;

		foreach (var property in properties)
		{
			if (m_filterName is null || m_filterName(property.Name))
			{
				if (builder.TextLength != oldTextLength)
					builder.AppendText(", ");
				builder.AppendText(builder.Syntax.NamedParameterPrefix);
				builder.AppendText(GetName(property.Name));
			}
		}

		if (builder.TextLength == oldTextLength)
			throw new InvalidOperationException($"The specified type has no remaining columns: {typeof(T).FullName}");
	}

	private string GetName(string name) => m_transformName is not null ? m_transformName(name) : name;

	private readonly Func<string, bool>? m_filterName;
	private readonly Func<string, string>? m_transformName;
}
