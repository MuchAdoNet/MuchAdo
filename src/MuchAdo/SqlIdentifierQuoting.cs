namespace MuchAdo;

/// <summary>
/// Specifies how to quote SQL identifiers.
/// </summary>
public enum SqlIdentifierQuoting
{
	/// <summary>
	/// Throws an exception if a SQL identifier is quoted.
	/// </summary>
	Throw,

	/// <summary>
	/// Quotes SQL identifiers with double quotes.
	/// </summary>
	DoubleQuotes,

	/// <summary>
	/// Quotes SQL identifiers with square brackets.
	/// </summary>
	Brackets,

	/// <summary>
	/// Quotes SQL identifiers with backticks.
	/// </summary>
	Backticks,
}
