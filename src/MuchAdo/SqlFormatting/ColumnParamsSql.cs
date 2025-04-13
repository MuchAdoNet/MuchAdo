namespace MuchAdo.SqlFormatting;

public sealed class ColumnParamsSql<T> : Sql
{
	public ColumnParamsSql<T> Where(Func<string, bool> filter) =>
		new(m_dto, m_filter is null ? filter : x => m_filter(x) && filter(x));

	internal ColumnParamsSql(T dto, Func<string, bool>? filter = null)
	{
		m_dto = dto;
		m_filter = filter;
	}

	internal override void Render(DbConnectorCommandBuilder builder)
	{
		var properties = DbDtoInfo.GetInfo<T>().Properties;
		if (properties.Count == 0)
			throw new InvalidOperationException($"The specified type has no columns: {typeof(T).FullName}");

		var filteredProperties = properties.AsEnumerable();
		if (m_filter is not null)
			filteredProperties = filteredProperties.Where(x => m_filter(x.Name));

		var oldTextLength = builder.TextLength;

		foreach (var filteredProperty in filteredProperties)
		{
			using var scope = builder.Prefix(builder.TextLength != oldTextLength ? ", " : "");
			builder.AppendParameterValue(null, m_dto, filteredProperty);
		}

		if (builder.TextLength == oldTextLength)
			throw new InvalidOperationException($"The specified type has no remaining columns: {typeof(T).FullName}");
	}

	private readonly T m_dto;
	private readonly Func<string, bool>? m_filter;
}
