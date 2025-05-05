using System.Text.RegularExpressions;

namespace MuchAdo.Ellipses;

/// <summary>
/// Methods for performing bulk inserts.
/// </summary>
public static class BulkInsertUtility
{
	/// <summary>
	/// Efficiently inserts multiple rows, in batches as necessary.
	/// </summary>
	public static int BulkInsert(this DbConnectorCommandBatch commandBatch, IEnumerable<SqlParamSource> rows, BulkInsertSettings? settings = null)
	{
		if (commandBatch.CommandCount != 1)
			throw new ArgumentException("Command batch must contain exactly one command.", nameof(commandBatch));

		var command = commandBatch.LastCommand;
		var commandText = command.Text ?? command.Sql!.ToString(commandBatch.Connector.SqlSyntax);

		var rowCount = 0;
		foreach (var (sql, parameters) in GetBulkInsertCommands(commandText, command.Parameters, rows, settings))
			rowCount += CreateBatchCommand(commandBatch, sql, parameters).Execute();
		return rowCount;
	}

	/// <summary>
	/// Efficiently inserts multiple rows, in batches as necessary.
	/// </summary>
	public static Task<int> BulkInsertAsync(this DbConnectorCommandBatch commandBatch, IEnumerable<SqlParamSource> rows, CancellationToken cancellationToken) =>
		commandBatch.BulkInsertAsync(rows, settings: null, cancellationToken);

	/// <summary>
	/// Efficiently inserts multiple rows, in batches as necessary.
	/// </summary>
	public static async Task<int> BulkInsertAsync(this DbConnectorCommandBatch commandBatch, IEnumerable<SqlParamSource> rows, BulkInsertSettings? settings = null, CancellationToken cancellationToken = default)
	{
		if (commandBatch.CommandCount != 1)
			throw new ArgumentException("Command batch must contain exactly one command.", nameof(commandBatch));

		var command = commandBatch.LastCommand;
		var commandText = command.Text ?? command.Sql!.ToString(commandBatch.Connector.SqlSyntax);

		var rowCount = 0;
		foreach (var (sql, parameters) in GetBulkInsertCommands(commandText, command.Parameters, rows, settings))
			rowCount += await CreateBatchCommand(commandBatch, sql, parameters).ExecuteAsync(cancellationToken).ConfigureAwait(false);
		return rowCount;
	}

	// internal for unit testing
	internal static IEnumerable<(string Sql, SqlParamSource Parameters)> GetBulkInsertCommands(string sql, SqlParamSource commonParameters, IEnumerable<SqlParamSource> rows, BulkInsertSettings? settings = null)
	{
		if (rows is null)
			throw new ArgumentNullException(nameof(rows));

		var valuesClauseMatches = s_valuesClauseRegex.Matches(sql);
		if (valuesClauseMatches.Count == 0)
			throw new ArgumentException("SQL does not contain 'VALUES (' followed by ')...'.", nameof(sql));
		if (valuesClauseMatches.Count > 1)
			throw new ArgumentException("SQL contains more than one 'VALUES (' followed by ')...'.", nameof(sql));

		var valuesClauseMatch = valuesClauseMatches[0];
		var tupleMatch = valuesClauseMatch.Groups[1];
		var sqlPrefix = sql.Substring(0, tupleMatch.Index);
		var sqlSuffix = sql.Substring(valuesClauseMatch.Index + valuesClauseMatch.Length);

		var tupleParts = s_parameterRegex.Split(tupleMatch.Value);
		var tupleParameters = new Dictionary<string, int[]>(StringComparer.OrdinalIgnoreCase);
		for (var index = 1; index < tupleParts.Length; index += 2)
		{
			var name = tupleParts[index];
			tupleParameters[name] = tupleParameters.TryGetValue(name, out var indices) ? [.. indices, index] : [index];
			name = name.Substring(1);
			tupleParameters[name] = tupleParameters.TryGetValue(name, out indices) ? [.. indices, index] : [index];
		}

		var maxParametersPerBatch = settings?.MaxParametersPerBatch ?? (settings?.MaxRowsPerBatch is null ? c_defaultMaxParametersPerBatch : int.MaxValue);
		if (maxParametersPerBatch < 1)
			throw new ArgumentException($"{nameof(settings.MaxParametersPerBatch)} setting must be positive.");

		var maxRowsPerBatch = settings?.MaxRowsPerBatch ?? int.MaxValue;
		if (maxRowsPerBatch < 1)
			throw new ArgumentException($"{nameof(settings.MaxRowsPerBatch)} setting must be positive.");

		var batchSqls = new List<string>();
		Dictionary<string, object?>? batchParameters = null;
		var rowParts = new string[tupleParts.Length];
		string GetBatchSql() => sqlPrefix + string.Join(", ", batchSqls) + sqlSuffix;

		foreach (var rowParameters in rows)
		{
			batchParameters ??= commonParameters.Enumerate().ToDictionary(x => x.Name, x => x.Value);

			var recordIndex = batchSqls.Count;
			Array.Copy(tupleParts, rowParts, tupleParts.Length);

			foreach (var rowParameter in rowParameters.Enumerate())
			{
				if (tupleParameters.TryGetValue(rowParameter.Name, out var indices))
				{
					foreach (var index in indices)
					{
						rowParts[index] = $"{rowParts[index]}_{recordIndex}";
						batchParameters[$"{rowParameter.Name}_{recordIndex}"] = rowParameter.Value;
					}
				}
			}

			batchSqls.Add(string.Concat(rowParts));

			if (batchSqls.Count == maxRowsPerBatch || batchParameters.Count + tupleParts.Length / 2 > maxParametersPerBatch)
			{
				yield return (GetBatchSql(), Sql.NamedParams(batchParameters));
				batchSqls.Clear();
				batchParameters = null;
			}
		}

		if (batchSqls.Count != 0)
			yield return (GetBatchSql(), Sql.NamedParams(batchParameters!));
	}

	private static DbConnectorCommandBatch CreateBatchCommand(DbConnectorCommandBatch commandBatch, string sql, SqlParamSource parameters)
	{
		var batchCommand = commandBatch.Connector.Command(sql, parameters);
		if (commandBatch.IsCached is { } isCached)
			batchCommand = batchCommand.Cache(isCached);
		if (commandBatch.IsPrepared is { } isPrepared)
			batchCommand = batchCommand.Prepare(isPrepared);
		if (commandBatch.Timeout is not null)
			batchCommand = batchCommand.WithTimeout(commandBatch.Timeout.Value);
		return batchCommand;
	}

	private const int c_defaultMaxParametersPerBatch = 999;

	private static readonly Regex s_valuesClauseRegex = new(
		@"\b[vV][aA][lL][uU][eE][sS]\s*(\(.*?\))\s*\.\.\.", RegexOptions.CultureInvariant | RegexOptions.Singleline | RegexOptions.RightToLeft);

	private static readonly Regex s_parameterRegex = new(@"([?@:]\w+)\b", RegexOptions.CultureInvariant);
}
