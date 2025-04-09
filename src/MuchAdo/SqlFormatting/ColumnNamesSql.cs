using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace MuchAdo.SqlFormatting;

[SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "Same name.")]
public sealed class ColumnNamesSql<T> : Sql
{
	public ColumnNamesSql<T> From(string tableName) =>
		new(tableName, m_filterName);

	public ColumnNamesSql<T> Where(Func<string, bool> nameMatches) =>
		new(m_tableName, m_filterName is null ? nameMatches : x => m_filterName(x) && nameMatches(x));

	internal ColumnNamesSql(string tableName = "", Func<string, bool>? filterName = null)
	{
		m_tableName = tableName;
		m_filterName = filterName;
	}

	internal override string Render(SqlContext context)
	{
		var properties = DbDtoInfo.GetInfo<T>().Properties;
		if (properties.Count == 0)
			throw new InvalidOperationException($"The specified type has no columns: {typeof(T).FullName}");

		var syntax = context.Syntax;
		var tablePrefix = m_tableName.Length == 0 ? "" : syntax.QuoteName(m_tableName) + ".";
		var useSnakeCase = syntax.SnakeCaseColumnNames;

		var filteredProperties = properties.AsEnumerable();
		if (m_filterName is not null)
			filteredProperties = filteredProperties.Where(x => m_filterName(x.Name));

		var text = string.Join(", ",
			filteredProperties.Select(x => tablePrefix + syntax.QuoteName(
				x.ColumnName ??
				(useSnakeCase ? ColumnNamesSql.SnakeCaseCache.GetOrAdd(x.Name, JsonNamingPolicy.SnakeCaseLower.ConvertName) : x.Name))));
		if (text.Length == 0)
			throw new InvalidOperationException($"The specified type has no remaining columns: {typeof(T).FullName}");
		return text;
	}

	private readonly string m_tableName;
	private readonly Func<string, bool>? m_filterName;
}

[SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "Same name.")]
internal static class ColumnNamesSql
{
	internal static ConcurrentDictionary<string, string> SnakeCaseCache { get; } = new();
}
