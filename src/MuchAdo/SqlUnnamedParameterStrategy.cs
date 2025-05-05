namespace MuchAdo;

/// <summary>
/// A strategy for handling unnamed SQL parameters.
/// </summary>
public sealed class SqlUnnamedParameterStrategy
{
	/// <summary>
	/// Uses named parameters with the specified prefix.
	/// </summary>
	public static SqlUnnamedParameterStrategy Named(string namePrefix) =>
		new() { NamedParameterNamePrefix = namePrefix };

	/// <summary>
	/// Uses numbered parameters with the specified prefix.
	/// </summary>
	public static SqlUnnamedParameterStrategy Numbered(string placeholderPrefix) =>
		new() { NumberedParameterPlaceholderPrefix = placeholderPrefix };

	/// <summary>
	/// Uses unnumbered parameters with the specified placeholder.
	/// </summary>
	public static SqlUnnamedParameterStrategy Unnumbered(string placeholder) =>
		new() { UnnumberedParameterPlaceholder = placeholder };

	internal string? NamedParameterNamePrefix { get; init; }

	internal string? NumberedParameterPlaceholderPrefix { get; init; }

	internal string? UnnumberedParameterPlaceholder { get; init; }

	private SqlUnnamedParameterStrategy()
	{
	}
}
