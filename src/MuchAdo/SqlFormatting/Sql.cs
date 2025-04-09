using System.Diagnostics.CodeAnalysis;

namespace MuchAdo.SqlFormatting;

/// <summary>
/// Encapsulates parameterized SQL.
/// </summary>
[SuppressMessage("Naming", "CA1724", Justification = "Conflicts with rarely-used System.Data.Sql namespace.")]
public abstract class Sql
{
	/// <summary>
	/// An empty SQL string.
	/// </summary>
	public static readonly Sql Empty = Raw("");

	/// <summary>
	/// Joins the specified SQL fragments with the AND operator.
	/// </summary>
	public static Sql And(params IEnumerable<Sql> sqls) => new BinaryOperatorSql(" and ", " AND ", AsReadOnlyList(sqls));

	/// <summary>
	/// Joins the specified SQL fragments with newlines.
	/// </summary>
	public static Sql Clauses(params IEnumerable<Sql> sqls) => Join("\n", sqls);

	/// <summary>
	/// Returns a comma-delimited list of column names for a DTO of the specified type.
	/// </summary>
	public static ColumnNamesSql<T> ColumnNames<T>() => new();

	/// <summary>
	/// Returns a comma-delimited list of column names for a DTO of the specified type.
	/// </summary>
	public static ColumnNamesSql<T> ColumnNames<T>(T dto) => new();

	/// <summary>
	/// Returns a comma-delimited list of arbitrarily-named parameters for the column values of the specified DTO.
	/// </summary>
	public static ColumnParamsSql<T> ColumnParams<T>(T dto) => new(dto ?? throw new ArgumentNullException(nameof(dto)));

	/// <summary>
	/// Concatenates SQL fragments.
	/// </summary>
	public static Sql Concat(params IEnumerable<Sql> sqls) =>
		new ConcatSql(AsReadOnlyList(sqls ?? throw new ArgumentNullException(nameof(sqls))));

	/// <summary>
	/// Returns a comma-delimited list of named parameters for the properties of the specified DTO.
	/// </summary>
	/// <remarks>The parameter names are the same as those used by the <c>Dto</c> methods of <see cref="DbParameters"/>.</remarks>
	public static DtoParamNamesSql<T> DtoParamNames<T>() => new();

	/// <summary>
	/// Returns a comma-delimited list of named parameters for the properties of the specified DTO.
	/// </summary>
	/// <remarks>The parameter names are the same as those used by the <c>Dto</c> methods of <see cref="DbParameters"/>.</remarks>
	public static DtoParamNamesSql<T> DtoParamNames<T>(T dto) => new();

	/// <summary>
	/// Creates SQL from a formatted string.
	/// </summary>
	public static Sql Format(SqlFormatStringHandler stringHandler) => stringHandler.ToSql();

	/// <summary>
	/// Creates SQL for a GROUP BY clause. If the SQLs are empty, the GROUP BY clause is omitted.
	/// </summary>
	public static Sql GroupBy(params IEnumerable<Sql> sqls) => new OptionalClauseSql("group by ", "GROUP BY ", Join(", ", sqls));

	/// <summary>
	/// Creates SQL for a HAVING clause. If the SQL is empty, the HAVING clause is omitted.
	/// </summary>
	public static Sql Having(Sql sql) => new OptionalClauseSql("having ", "HAVING ", sql);

	/// <summary>
	/// Joins SQL fragments with the specified separator.
	/// </summary>
	/// <remarks>Empty SQL fragments are ignored.</remarks>
	public static Sql Join(string separator, params IEnumerable<Sql> sqls) =>
		new JoinSql(separator ?? throw new ArgumentNullException(nameof(separator)), AsReadOnlyList(sqls ?? throw new ArgumentNullException(nameof(sqls))));

	/// <summary>
	/// Creates SQL for an arbitrarily-named parameter with the specified fragment of a LIKE pattern followed by a trailing <c>%</c>.
	/// </summary>
	/// <remarks>This SQL fragment escapes <c>%</c> and <c>_</c> in the prefix with <c>\</c>. Depending on the database
	/// and its settings, <c>escape '\'</c> may be needed after the parameter.</remarks>
	public static Sql LikeParamStartsWith(string prefix) => new LikeParamStartsWithSql(prefix ?? throw new ArgumentNullException(nameof(prefix)));

	/// <summary>
	/// Creates SQL for a comma-delimited list of SQL fragments.
	/// </summary>
	/// <remarks>Empty SQL fragments are ignored. Since it would otherwise result in a confusing SQL syntax error, an <see cref="InvalidOperationException" />
	/// is thrown if the SQL fragments are missing or all empty. Use <c>Sql.Join(", ", sqls)</c> to permit an empty SQL fragment.</remarks>
	public static Sql List(params IEnumerable<Sql> sqls) => JoinOrThrow(", ", sqls, "Sql.List was empty.");

	/// <summary>
	/// Creates SQL for a quoted identifier.
	/// </summary>
	public static Sql Name(string identifier) => new NameSql(identifier ?? throw new ArgumentNullException(nameof(identifier)));

	/// <summary>
	/// Joins the specified SQL fragments with the OR operator.
	/// </summary>
	public static Sql Or(params IEnumerable<Sql> sqls) => new BinaryOperatorSql(" or ", " OR ", AsReadOnlyList(sqls));

	/// <summary>
	/// Creates SQL for an ORDER BY clause. If the SQLs are empty, the ORDER BY clause is omitted.
	/// </summary>
	public static Sql OrderBy(params IEnumerable<Sql> sqls) => new OptionalClauseSql("order by ", "ORDER BY ", Join(", ", sqls));

	/// <summary>
	/// Creates SQL for an arbitrarily-named parameter with the specified value.
	/// </summary>
	public static Sql Param<T>(T value)
	{
		if (value is Sql)
			throw new ArgumentException("Parameters should not be created from Sql instances.", nameof(value));
		return new ParamSql<T>(value);
	}

	/// <summary>
	/// Creates SQL for a named parameter with the specified value.
	/// </summary>
	public static Sql NamedParam<T>(string name, T value)
	{
		if (value is Sql)
			throw new ArgumentException("Parameters should not be created from Sql instances.", nameof(value));
		return new NamedParamSql<T>(name, value);
	}

	/// <summary>
	/// Creates SQL for a comma-delimted list of arbitrarily-named parameters with the specified values.
	/// </summary>
	/// <remarks>Empty SQL fragments are ignored. Since it would otherwise result in a confusing SQL syntax error, an <see cref="InvalidOperationException" />
	/// is thrown if the collection of values is empty. Use <c>Sql.Join(", ", values.Select(Sql.Param))")</c> to allow an empty collection.</remarks>
	public static Sql ParamList<T>(IEnumerable<T> values) => JoinOrThrow(", ", values.Select(Param), "Sql.ParamList was empty.");

	/// <summary>
	/// Creates SQL for a comma-delimted list of arbitrarily-named parameters with the specified values, surrounded by parentheses.
	/// </summary>
	/// <remarks>Empty SQL fragments are ignored. Since it would otherwise result in a confusing SQL syntax error, an <see cref="InvalidOperationException" />
	/// is thrown if the collection of values is empty. Use <c>Sql.Format($"({Sql.Join(", ", values.Select(Sql.Param))})")</c> to permit an empty tuple.</remarks>
	public static Sql ParamTuple<T>(IEnumerable<T> values) => Format($"({JoinOrThrow(", ", values.Select(Param), "Sql.ParamTuple was empty.")})");

	/// <summary>
	/// Creates SQL from a raw string.
	/// </summary>
	public static Sql Raw(string text) => new RawSql(text ?? throw new ArgumentNullException(nameof(text)));

	/// <summary>
	/// Creates SQL for a comma-delimited list of SQL fragments, surrounded by parentheses.
	/// </summary>
	/// <remarks>Empty SQL fragments are ignored. Since it would otherwise result in a confusing SQL syntax error, an <see cref="InvalidOperationException" />
	/// is thrown if the SQL fragments are missing or all empty. Use <c>Sql.Format($"({Sql.Join(", ", sqls)})")</c> to permit an empty tuple.</remarks>
	public static Sql Tuple(params IEnumerable<Sql> sqls) => Format($"({JoinOrThrow(", ", sqls, "Sql.Tuple was empty.")})");

	/// <summary>
	/// Creates SQL for a WHERE clause. If the SQL is empty, the WHERE clause is omitted.
	/// </summary>
	public static Sql Where(Sql sql) => new OptionalClauseSql("where ", "WHERE ", sql);

	/// <summary>
	/// Concatenates two SQL fragments.
	/// </summary>
	[SuppressMessage("Usage", "CA2225:Operator overloads have named alternates", Justification = "Use Concat.")]
	public static Sql operator +(Sql a, Sql b) => new AddSql(a, b);

	/// <inheritdoc />
	public override string ToString() => SqlSyntax.Ansi.Render(this).Text;

	internal abstract string Render(SqlContext context);

	private static Sql JoinOrThrow(string separator, IEnumerable<Sql> sqls, string throwMessageIfEmpty) =>
		new JoinSql(separator ?? throw new ArgumentNullException(nameof(separator)), AsReadOnlyList(sqls ?? throw new ArgumentNullException(nameof(sqls))), throwMessageIfEmpty);

	private static IReadOnlyList<T> AsReadOnlyList<T>(IEnumerable<T> items) => (items as IReadOnlyList<T>) ?? items.ToList();

	private sealed class AddSql(Sql a, Sql b) : Sql
	{
		internal override string Render(SqlContext context) => a.Render(context) + b.Render(context);
	}

	private sealed class BinaryOperatorSql(string lowercase, string uppercase, IReadOnlyList<Sql> sqls) : Sql
	{
		private bool HasMultipleSqls => sqls.Count > 1;

		internal override string Render(SqlContext context)
		{
			var rawSqls = sqls
				.Select(x => (RawSql: x.Render(context), NeedsParens: x is BinaryOperatorSql { HasMultipleSqls: true }))
				.Where(x => x.RawSql.Length != 0)
				.Select(x => x.NeedsParens ? $"({x.RawSql})" : x.RawSql)
				.ToList();
			return string.Join(context.Syntax.LowerCaseKeywords ? lowercase : uppercase, rawSqls);
		}
	}

	private sealed class ConcatSql(IReadOnlyList<Sql> sqls) : Sql
	{
		internal override string Render(SqlContext context) => string.Concat(sqls.Select(x => x.Render(context)));
	}

	private sealed class JoinSql(string separator, IReadOnlyList<Sql> sqls, string? throwMessageIfEmpty = null) : Sql
	{
		internal override string Render(SqlContext context)
		{
			var sql = string.Join(separator, sqls.Select(x => x.Render(context)).Where(x => x.Length != 0));
			if (throwMessageIfEmpty is not null && sql.Length == 0)
				throw new InvalidOperationException(throwMessageIfEmpty);
			return sql;
		}
	}

	private sealed class LikeParamStartsWithSql(string prefix) : Sql
	{
		internal override string Render(SqlContext context) => context.RenderParameter(key: this, value: context.Syntax.EscapeLikeFragment(prefix) + "%");
	}

	private sealed class NameSql(string identifier) : Sql
	{
		internal override string Render(SqlContext context) => context.Syntax.QuoteName(identifier);
	}

	private sealed class OptionalClauseSql(string lowercase, string uppercase, Sql sql) : Sql
	{
		internal override string Render(SqlContext context)
		{
			var rawSql = sql.Render(context);
			return rawSql.Length == 0 ? "" : (context.Syntax.LowerCaseKeywords ? lowercase : uppercase) + rawSql;
		}
	}

	private sealed class ParamSql<T>(T value) : Sql
	{
		internal override string Render(SqlContext context) => context.RenderParameter(key: this, value: value);
	}

	private sealed class NamedParamSql<T>(string name, T value) : Sql
	{
		internal override string Render(SqlContext context)
		{
			context.AddParameters(DbParameters.Create(name, value));
			return context.Syntax.ParameterStart + name;
		}
	}

	private sealed class RawSql(string text) : Sql
	{
		internal override string Render(SqlContext context) => text;
	}
}
