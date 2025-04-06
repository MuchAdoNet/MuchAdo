using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace MuchAdo.SqlFormatting;

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
				(useSnakeCase ? s_snakeCaseCache.GetOrAdd(x.Name, ToSnakeCase) : x.Name))));
		if (text.Length == 0)
			throw new InvalidOperationException($"The specified type has no remaining columns: {typeof(T).FullName}");
		return text;
	}

	private static string ToSnakeCase(string value) => string.Join("_", s_word.Matches(value).Cast<Match>().Select(x => x.Value.ToLowerInvariant()));

	private static readonly Regex s_word = new Regex("[A-Z]([A-Z]*(?![a-z])|[a-z]*)|[a-z]+|[0-9]+", RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture);

	private static readonly ConcurrentDictionary<string, string> s_snakeCaseCache = new();

	private readonly string m_tableName;
	private readonly Func<string, bool>? m_filterName;
}
