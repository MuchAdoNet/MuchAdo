namespace MuchAdo;

/// <summary>
/// Settings when creating a <see cref="DbDataMapper"/>.
/// </summary>
public class DbDataMapperSettings
{
	/// <summary>
	/// True to allow strings to be mapped to enums.
	/// </summary>
	public bool AllowStringToEnum { get; init; }
}
