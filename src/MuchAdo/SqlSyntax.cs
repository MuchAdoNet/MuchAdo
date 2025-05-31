namespace MuchAdo;

/// <summary>
/// The syntax used by a particular SQL dialect.
/// </summary>
public sealed class SqlSyntax
{
	/// <summary>
	/// The default syntax.
	/// </summary>
	/// <remarks>The default syntax does not support quoted identifiers, since the syntax
	/// is highly dependent on the type of database and its settings.</remarks>
	public static SqlSyntax Default { get; } = new();

	/// <summary>
	/// The syntax for MySQL.
	/// </summary>
	public static SqlSyntax MySql { get; } = Default
		.WithIdentifierQuoting(SqlIdentifierQuoting.Backticks)
		.WithUnnamedParameterStrategy(SqlUnnamedParameterStrategy.Unnumbered("?"));

	/// <summary>
	/// The syntax for PostgreSQL.
	/// </summary>
	public static SqlSyntax Postgres { get; } = Default
		.WithIdentifierQuoting(SqlIdentifierQuoting.DoubleQuotes)
		.WithUnnamedParameterStrategy(SqlUnnamedParameterStrategy.Numbered("$"));

	/// <summary>
	/// The syntax for Microsoft SQL Server.
	/// </summary>
	public static SqlSyntax SqlServer { get; } = Default.WithIdentifierQuoting(SqlIdentifierQuoting.Brackets);

	/// <summary>
	/// The syntax for SQLite.
	/// </summary>
	public static SqlSyntax Sqlite { get; } = Default.WithIdentifierQuoting(SqlIdentifierQuoting.DoubleQuotes);

	/// <summary>
	/// Indicates how identifiers should be quoted.
	/// </summary>
	public SqlIdentifierQuoting IdentifierQuoting { get; private init; }

	/// <summary>
	/// Creates a new syntax with the specified identifier quoting.
	/// </summary>
	public SqlSyntax WithIdentifierQuoting(SqlIdentifierQuoting value) => new(this) { IdentifierQuoting = value };

	/// <summary>
	/// True if snake case should be used when generating column names.
	/// </summary>
	public bool SnakeCaseColumnNames { get; private init; }

	/// <summary>
	/// Creates a new syntax with the specified snake case column names setting.
	/// </summary>
	public SqlSyntax WithSnakeCaseColumnNames(bool value = true) => new(this) { SnakeCaseColumnNames = value };

	/// <summary>
	/// True if lowercase should be used when generating SQL keywords.
	/// </summary>
	public bool LowercaseKeywords { get; private init; }

	/// <summary>
	/// Creates a new syntax with the specified lowercase keywords setting.
	/// </summary>
	public SqlSyntax WithLowercaseKeywords(bool value = true) => new(this) { LowercaseKeywords = value };

	/// <summary>
	/// The prefix of a named parameter.
	/// </summary>
	public string NamedParameterPrefix { get; private init; }

	/// <summary>
	/// Creates a new syntax with the specified named parameter prefix.
	/// </summary>
	public SqlSyntax WithNamedParameterChar(string value) => new(this) { NamedParameterPrefix = value };

	/// <summary>
	/// The strategy for unnamed parameters.
	/// </summary>
	public SqlUnnamedParameterStrategy UnnamedParameterStrategy { get; private init; }

	/// <summary>
	/// Creates a new syntax with the specified strategy for unnamed parameters.
	/// </summary>
	public SqlSyntax WithUnnamedParameterStrategy(SqlUnnamedParameterStrategy value) => new(this) { UnnamedParameterStrategy = value };

	/// <summary>
	/// Escapes a fragment of a LIKE pattern.
	/// </summary>
	/// <returns>The string fragment, with wildcard characters such as <c>%</c>
	/// and <c>_</c> escaped as needed. This string is not raw SQL, but rather
	/// a fragment of a LIKE pattern that should be concatenated with the rest of
	/// the LIKE pattern and sent to the database via a string parameter.</returns>
	internal string EscapeLikeFragment(string fragment)
	{
		const string escapeString = @"\";
		return (fragment ?? throw new ArgumentNullException(nameof(fragment)))
			.ReplaceOrdinal(escapeString, escapeString + escapeString)
			.ReplaceOrdinal("%", escapeString + "%")
			.ReplaceOrdinal("_", escapeString + "_");
	}

	/// <summary>
	/// Quotes the specified identifier so that it can be used as a schema/table/column name
	/// even if it matches a keyword or has special characters.
	/// </summary>
	internal (string Start, string Escaped, string End) QuoteName(string name) => IdentifierQuoting switch
	{
		SqlIdentifierQuoting.DoubleQuotes => ("\"", EscapeName(name, '"'), "\""),
		SqlIdentifierQuoting.Brackets => ("[", EscapeName(name, ']'), "]"),
		SqlIdentifierQuoting.Backticks => ("`", EscapeName(name, '`'), "`"),
		_ => throw new InvalidOperationException("The default SqlSyntax does not support quoted identifiers. Use a SqlSyntax that matches your database."),
	};

	private SqlSyntax()
	{
		IdentifierQuoting = SqlIdentifierQuoting.Throw;
		SnakeCaseColumnNames = false;
		LowercaseKeywords = false;
		NamedParameterPrefix = "@";
		UnnamedParameterStrategy = SqlUnnamedParameterStrategy.Named("ado");
	}

	private SqlSyntax(SqlSyntax source)
	{
		IdentifierQuoting = source.IdentifierQuoting;
		SnakeCaseColumnNames = source.SnakeCaseColumnNames;
		LowercaseKeywords = source.LowercaseKeywords;
		NamedParameterPrefix = source.NamedParameterPrefix;
		UnnamedParameterStrategy = source.UnnamedParameterStrategy;
	}

	private static string EscapeName(string name, char nameQuoteEnd) =>
		name.ContainsOrdinal(nameQuoteEnd)
			? name.ReplaceOrdinal(new string(nameQuoteEnd, 1), new string(nameQuoteEnd, 2))
			: name;
}
