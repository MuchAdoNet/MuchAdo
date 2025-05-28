using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace MuchAdo.Sources;

[SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "Same name.")]
public sealed class DtoColumnNamesSqlSource<T> : SqlSource
{
	public DtoColumnNamesSqlSource<T> From(string tableName) =>
		new(tableName, m_filterName);

	public DtoColumnNamesSqlSource<T> Where(Func<string, bool> nameMatches) =>
		new(m_tableName, m_filterName is null ? nameMatches : x => m_filterName(x) && nameMatches(x));

	internal DtoColumnNamesSqlSource(string tableName = "", Func<string, bool>? filterName = null)
	{
		m_tableName = tableName;
		m_filterName = filterName;
	}

	internal override void Render(DbConnectorCommandBuilder builder)
	{
		var properties = DbDtoInfo.GetInfo<T>().Properties;
		if (properties.Count == 0)
			throw new InvalidOperationException($"The specified type has no columns: {typeof(T).FullName}");

		var syntax = builder.Syntax;
		var hasTableName = m_tableName.Length != 0;
		var tablePrefixParts = hasTableName ? syntax.QuoteName(m_tableName) : default;
		var useSnakeCase = syntax.SnakeCaseColumnNames;

		var firstProperty = true;
		foreach (var property in properties)
		{
			if (m_filterName is null || m_filterName(property.Name))
			{
				if (firstProperty)
					firstProperty = false;
				else
					builder.AppendText(", ");

				if (hasTableName)
				{
					builder.AppendText(tablePrefixParts.Start!);
					builder.AppendText(tablePrefixParts.Escaped!);
					builder.AppendText(tablePrefixParts.End!);
					builder.AppendText(".");
				}

				var columnNameParts = syntax.QuoteName(property.ColumnName ??
					(useSnakeCase ? ColumnNamesSql.SnakeCaseCache.GetOrAdd(property.Name, JsonNamingPolicy.SnakeCaseLower.ConvertName) : property.Name));
				builder.AppendText(columnNameParts.Start);
				builder.AppendText(columnNameParts.Escaped);
				builder.AppendText(columnNameParts.End);
			}
		}
	}

	private readonly string m_tableName;
	private readonly Func<string, bool>? m_filterName;
}

[SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "Same name.")]
internal static class ColumnNamesSql
{
	internal static ConcurrentDictionary<string, string> SnakeCaseCache { get; } = new();
}
