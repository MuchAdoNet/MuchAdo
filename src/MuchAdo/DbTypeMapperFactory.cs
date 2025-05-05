namespace MuchAdo;

/// <summary>
/// Creates <see cref="DbTypeMapper{T}" /> for supported types.
/// </summary>
public abstract class DbTypeMapperFactory
{
	/// <summary>
	/// Returns a <see cref="DbTypeMapper{T}"/> for the specified type, or null if the type is not supported by this factory.
	/// </summary>
	public abstract DbTypeMapper<T>? TryCreateTypeMapper<T>(DbDataMapper dataMapper);
}
