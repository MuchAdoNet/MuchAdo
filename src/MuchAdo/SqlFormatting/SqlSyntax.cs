using System.Diagnostics.CodeAnalysis;

namespace MuchAdo.SqlFormatting;

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
	/// The syntax for ANSI SQL.
	/// </summary>
	public static SqlSyntax Ansi { get; } = Default.WithIdentifierQuoting(SqlIdentifierQuoting.DoubleQuotes);

	/// <summary>
	/// The syntax for MySQL.
	/// </summary>
	public static SqlSyntax MySql { get; } = Default.WithIdentifierQuoting(SqlIdentifierQuoting.Backticks);

	/// <summary>
	/// The syntax for PostgreSQL.
	/// </summary>
	public static SqlSyntax Postgres { get; } = Default.WithIdentifierQuoting(SqlIdentifierQuoting.DoubleQuotes);

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
	/// The start character used to indicate a parameter.
	/// </summary>
	public char ParameterStart { get; private init; }

	/// <summary>
	/// Creates a new syntax with the specified parameter start character.
	/// </summary>
	public SqlSyntax WithParameterStart(char value) => new(this) { ParameterStart = value };

	/// <summary>
	/// The prefix for unnamed parameters.
	/// </summary>
	public string UnnamedParameterPrefix { get; private init; }

	/// <summary>
	/// Creates a new syntax with the specified prefix for unnamed parameters.
	/// </summary>
	public SqlSyntax WithUnnamedParameterPrefix(string value) => new(this) { UnnamedParameterPrefix = value };

	/// <summary>
	/// Escapes a fragment of a LIKE pattern.
	/// </summary>
	/// <returns>The string fragment, with wildcard characters such as <c>%</c>
	/// and <c>_</c> escaped as needed. This string is not raw SQL, but rather
	/// a fragment of a LIKE pattern that should be concatenated with the rest of
	/// the LIKE pattern and sent to the database via a string parameter.</returns>
	[SuppressMessage("Globalization", "CA1307:Specify StringComparison for clarity", Justification = ".NET Standard 2.0")]
	public string EscapeLikeFragment(string fragment)
	{
		const string escapeString = @"\";
		return (fragment ?? throw new ArgumentNullException(nameof(fragment)))
			.Replace(escapeString, escapeString + escapeString)
			.Replace("%", escapeString + "%")
			.Replace("_", escapeString + "_");
	}

	/// <summary>
	/// Quotes the specified identifier so that it can be used as a schema/table/column name
	/// even if it matches a keyword or has special characters.
	/// </summary>
	public string QuoteName(string name) => IdentifierQuoting switch
	{
		SqlIdentifierQuoting.DoubleQuotes => QuoteName(name, '"', '"'),
		SqlIdentifierQuoting.Brackets => QuoteName(name, '[', ']'),
		SqlIdentifierQuoting.Backticks => QuoteName(name, '`', '`'),
		_ => throw new InvalidOperationException("The default SqlSyntax does not support quoted identifiers. Use a SqlSyntax that matches your database."),
	};

	/// <summary>
	/// Renders SQL as text and parameters.
	/// </summary>
	public (string Text, DbParameters Parameters) Render(Sql sql)
	{
		var context = new SqlContext(this);
		var text = (sql ?? throw new ArgumentNullException(nameof(sql))).Render(context);
		return (text, context.Parameters);
	}

	private SqlSyntax()
	{
		IdentifierQuoting = SqlIdentifierQuoting.Throw;
		ParameterStart = '@';
		SnakeCaseColumnNames = false;
		LowercaseKeywords = false;
		UnnamedParameterPrefix = "ado";
	}

	private SqlSyntax(SqlSyntax source)
	{
		IdentifierQuoting = source.IdentifierQuoting;
		SnakeCaseColumnNames = source.SnakeCaseColumnNames;
		LowercaseKeywords = source.LowercaseKeywords;
		ParameterStart = source.ParameterStart;
		UnnamedParameterPrefix = source.UnnamedParameterPrefix;
	}

	private static string QuoteName(string name, char nameQuoteStart, char nameQuoteEnd) =>
		nameQuoteStart +
		(name.ContainsOrdinal(nameQuoteEnd)
			? name.ReplaceOrdinal(new string(nameQuoteEnd, 1), new string(nameQuoteEnd, 2))
			: name) +
		nameQuoteEnd;
}
