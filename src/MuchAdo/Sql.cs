using System.Diagnostics.CodeAnalysis;
using MuchAdo.Sources;

namespace MuchAdo;

/// <summary>
/// Encapsulates parameterized SQL.
/// </summary>
[SuppressMessage("Naming", "CA1724", Justification = "Conflicts with rarely-used System.Data.Sql namespace.")]
public static class Sql
{
	/// <summary>
	/// Empty SQL/parameters.
	/// </summary>
	public static readonly SqlParamSource Empty = new EmptySqlParamSource();

	/// <summary>
	/// Joins the specified SQL fragments with the AND operator.
	/// </summary>
	public static SqlSource And(params IEnumerable<SqlSource> sqls) => new AndOperatorSqlSource(sqls);

	/// <summary>
	/// Joins the specified SQL fragments with newlines.
	/// </summary>
	public static SqlSource Clauses(params IEnumerable<SqlSource> sqls) => new ClausesSqlSource(sqls);

	/// <summary>
	/// Concatenates SQL fragments.
	/// </summary>
	public static SqlSource Concat(params IEnumerable<SqlSource> sqls) => new ConcatSqlSource(sqls);

	/// <summary>
	/// Returns a comma-separated list of column names corresponding to the properties of a DTO of the specified type.
	/// </summary>
	public static DtoColumnNamesSqlSource<T> DtoColumnNames<T>() => new();

	/// <summary>
	/// Returns a comma-separated list of column names corresponding to the properties of a DTO of the specified type.
	/// </summary>
	public static DtoColumnNamesSqlSource<T> DtoColumnNames<T>(T dto) => new();

	/// <summary>
	/// Returns named parameters for the properties of the specified DTO.
	/// </summary>
	public static SqlParamSource DtoNamedParams<T>(T dto) => new DtoNamedSqlParamSource<T>(dto);

	/// <summary>
	/// Returns a comma-separated list of named parameter placeholders for the properties of the specified DTO.
	/// </summary>
	public static DtoParamNamesSqlSource<T> DtoParamNames<T>() => new();

	/// <summary>
	/// Returns a comma-separated list of named parameter placeholders for the properties of the specified DTO.
	/// </summary>
	public static DtoParamNamesSqlSource<T> DtoParamNames<T>(T dto) => new();

	/// <summary>
	/// Returns unnamed parameters for the property values of the specified DTO.
	/// </summary>
	public static DtoSqlParamSource<T> DtoParams<T>(T dto) => new(dto ?? throw new ArgumentNullException(nameof(dto)));

	/// <summary>
	/// Creates SQL from a formatted string.
	/// </summary>
	public static SqlSource Format(SqlFormatStringHandler stringHandler) => stringHandler.ToSqlSource();

	/// <summary>
	/// Creates SQL for a GROUP BY clause. If the SQL is empty, the GROUP BY clause is omitted.
	/// </summary>
	public static SqlSource GroupBy(SqlSource sql) => new GroupByClauseSqlSource(sql);

	/// <summary>
	/// Creates SQL for a GROUP BY clause. If the SQLs are empty, the GROUP BY clause is omitted.
	/// </summary>
	public static SqlSource GroupBy(params IEnumerable<SqlSource> sqls) => GroupBy(List(sqls));

	/// <summary>
	/// Creates SQL for a HAVING clause. If the SQL is empty, the HAVING clause is omitted.
	/// </summary>
	public static SqlSource Having(SqlSource sql) => new HavingClauseSqlSource(sql);

	/// <summary>
	/// Joins SQL fragments with the specified separator.
	/// </summary>
	/// <remarks>Empty SQL fragments are ignored.</remarks>
	public static SqlSource Join(string separator, params IEnumerable<SqlSource> sqls) =>
		new JoinSqlSource(separator ?? throw new ArgumentNullException(nameof(separator)), sqls);

	/// <summary>
	/// Creates SQL for an unnamed parameter set to a LIKE pattern ending with <c>%</c>.
	/// </summary>
	/// <remarks>This SQL fragment escapes <c>%</c> and <c>_</c> in the prefix with <c>\</c>. Depending on the database
	/// and its settings, <c>escape '\'</c> may be needed after the parameter.</remarks>
	public static SqlSource LikeParamStartsWith(string prefix) => new LikeParamSqlSource(escape => $"{escape(prefix)}%");

	/// <summary>
	/// Creates SQL for an unnamed parameter set to a LIKE pattern starting with <c>%</c>.
	/// </summary>
	/// <remarks>This SQL fragment escapes <c>%</c> and <c>_</c> in the suffix with <c>\</c>. Depending on the database
	/// and its settings, <c>escape '\'</c> may be needed after the parameter.</remarks>
	public static SqlSource LikeParamEndsWith(string suffix) => new LikeParamSqlSource(escape => $"%{escape(suffix)}");

	/// <summary>
	/// Creates SQL for an unnamed parameter set to a LIKE pattern starting and ending with <c>%</c>.
	/// </summary>
	/// <remarks>This SQL fragment escapes <c>%</c> and <c>_</c> in the substring with <c>\</c>. Depending on the database
	/// and its settings, <c>escape '\'</c> may be needed after the parameter.</remarks>
	public static SqlSource LikeParamContains(string substring) => new LikeParamSqlSource(escape => $"%{escape(substring)}%");

	/// <summary>
	/// Creates SQL for an unnamed parameter set to an arbitrary LIKE pattern.
	/// </summary>
	/// <remarks>Use the provided delegate to escape the portions of the LIKE pattern that need escaping. Depending on the database
	/// and its settings, <c>escape '\'</c> may be needed after the parameter.</remarks>
	public static SqlSource LikeParam(Func<Func<string, string>, string> escaper) => new LikeParamSqlSource(escaper);

	/// <summary>
	/// Creates SQL for a comma-separated list of SQL fragments.
	/// </summary>
	/// <remarks>Empty SQL fragments are ignored.</remarks>
	public static SqlSource List(params IEnumerable<SqlSource> sqls) => new ListSqlSource(sqls);

	/// <summary>
	/// Creates SQL for a quoted identifier.
	/// </summary>
	public static SqlSource Name(string identifier) => new NameSqlSource(identifier ?? throw new ArgumentNullException(nameof(identifier)));

	/// <summary>
	/// Creates a named parameter with the specified value.
	/// </summary>
	public static SqlParam<T> NamedParam<T>(string name, T value) => new NamedSqlParam<T>(name, value);

	/// <summary>
	/// Creates a named parameter with the specified value and type.
	/// </summary>
	public static SqlParam<T> NamedParam<T>(string name, T value, SqlParamType? type) =>
		type is null ? new NamedSqlParam<T>(name, value) : new NamedTypedSqlParam<T>(name, value, type);

	/// <summary>
	/// Creates named parameters from tuples.
	/// </summary>
	public static SqlParamSource NamedParams<T>(params IEnumerable<(string Name, T Value)> parameters) =>
		new TuplesSqlParamSource<T>(parameters);

	/// <summary>
	/// Creates named parameters from a dictionary.
	/// </summary>
	public static SqlParamSource NamedParams<T>(IEnumerable<KeyValuePair<string, T>> parameters) =>
		new DictionarySqlParamSource<T>(parameters);

	/// <summary>
	/// Joins the specified SQL fragments with the OR operator.
	/// </summary>
	public static SqlSource Or(params IEnumerable<SqlSource> sqls) => new OrOperatorSqlSource(sqls);

	/// <summary>
	/// Creates SQL for an ORDER BY clause. If the SQL is empty, the ORDER BY clause is omitted.
	/// </summary>
	public static SqlSource OrderBy(SqlSource sql) => new OrderByClauseSqlSource(sql);

	/// <summary>
	/// Creates SQL for an ORDER BY clause. If the SQLs are empty, the ORDER BY clause is omitted.
	/// </summary>
	public static SqlSource OrderBy(params IEnumerable<SqlSource> sqls) => OrderBy(List(sqls));

	/// <summary>
	/// Creates an unnamed parameter with the specified value.
	/// </summary>
	public static SqlParam<T> Param<T>(T value) => new(value);

	/// <summary>
	/// Creates an unnamed parameter with the specified value and type.
	/// </summary>
	public static SqlParam<T> Param<T>(T value, SqlParamType? type) => type is null ? new SqlParam<T>(value) : new TypedSqlParam<T>(value, type);

	/// <summary>
	/// Creates unnamed parameters with the specified values.
	/// </summary>
	public static SqlParamSource Params<T>(IEnumerable<T> values) =>
		new ParamsSqlParamSource<T>(values ?? throw new ArgumentNullException(nameof(values)));

	/// <summary>
	/// Creates a comma-separated list of unnamed parameters for the specified values, surrounded by parentheses.
	/// </summary>
	public static SqlSource ParamSet<T>(IEnumerable<T> values) => Format($"({Params(values)})");

	/// <summary>
	/// Creates SQL from a raw string.
	/// </summary>
	public static SqlSource Raw(string text) => new RawSqlSource(text ?? throw new ArgumentNullException(nameof(text)));

	/// <summary>
	/// Creates an unnamed parameter with the specified value.
	/// </summary>
	/// <remarks>If the same object is used multiple times in the same command, the same parameter is used, if possible.</remarks>
	public static SqlParam<T> RepeatParam<T>(T value) => new RepeatSqlParam<T>(value);

	/// <summary>
	/// Creates an unnamed parameter with the specified value.
	/// </summary>
	/// <remarks>If the same object is used multiple times in the same command, the same parameter is used, if possible.</remarks>
	public static SqlParam<T> RepeatParam<T>(T value, SqlParamType? type) =>
		type is null ? new RepeatSqlParam<T>(value) : new RepeatTypedSqlParam<T>(value, type);

	/// <summary>
	/// Creates SQL for a comma-separated list of SQL fragments, surrounded by parentheses.
	/// </summary>
	public static SqlSource Tuple(params IEnumerable<SqlSource> sqls) => Format($"({List(sqls)})");

	/// <summary>
	/// Creates SQL for a WHERE clause. If the SQL is empty, the WHERE clause is omitted.
	/// </summary>
	public static SqlSource Where(SqlSource sql) => new WhereClauseSqlSource(sql);
}
