namespace MuchAdo;

/// <summary>
/// Settings when creating a <see cref="DefaultDbTypeMapperFactory"/>.
/// </summary>
public class DefaultDbTypeMapperSettings
{
	/// <summary>
	/// True to allow strings to be mapped to enums.
	/// </summary>
	public bool AllowStringToEnum { get; init; }
}
