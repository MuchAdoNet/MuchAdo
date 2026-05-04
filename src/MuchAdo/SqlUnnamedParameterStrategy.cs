namespace MuchAdo;

/// <summary>
/// A strategy for handling unnamed SQL parameters.
/// </summary>
public sealed class SqlUnnamedParameterStrategy
{
	/// <summary>
	/// Uses named parameters with the specified prefix.
	/// </summary>
	public static SqlUnnamedParameterStrategy Named(string namePrefix)
	{
		if (namePrefix is null)
			throw new ArgumentNullException(nameof(namePrefix));
		if (namePrefix.Length == 0)
			throw new ArgumentException("Name prefix cannot be empty.", nameof(namePrefix));

		return new() { NamedParameterNamePrefix = namePrefix };
	}

	/// <summary>
	/// Uses numbered parameters with the specified prefix.
	/// </summary>
	public static SqlUnnamedParameterStrategy Numbered(string placeholderPrefix)
	{
		if (placeholderPrefix is null)
			throw new ArgumentNullException(nameof(placeholderPrefix));
		if (placeholderPrefix.Length == 0)
			throw new ArgumentException("Placeholder prefix cannot be empty.", nameof(placeholderPrefix));

		return new() { NumberedParameterPlaceholderPrefix = placeholderPrefix };
	}

	/// <summary>
	/// Uses unnumbered parameters with the specified placeholder.
	/// </summary>
	public static SqlUnnamedParameterStrategy Unnumbered(string placeholder)
	{
		if (placeholder is null)
			throw new ArgumentNullException(nameof(placeholder));
		if (placeholder.Length == 0)
			throw new ArgumentException("Placeholder cannot be empty.", nameof(placeholder));

		return new() { UnnumberedParameterPlaceholder = placeholder };
	}

	internal string? NamedParameterNamePrefix { get; init; }

	internal string? NumberedParameterPlaceholderPrefix { get; init; }

	internal string? UnnumberedParameterPlaceholder { get; init; }

	private SqlUnnamedParameterStrategy()
	{
	}
}
