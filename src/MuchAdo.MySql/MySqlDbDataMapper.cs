namespace MuchAdo.MySql;

/// <summary>
/// Provides a <see cref="DbDataMapper"/> with MySQL type mappers.
/// </summary>
public static class MySqlDbDataMapper
{
	/// <summary>
	/// A <see cref="DbDataMapper"/> with MySQL type mappers.
	/// </summary>
	public static DbDataMapper Default { get; } = DbDataMapper.Default.WithMySqlTypeMapperFactory();

	/// <summary>
	/// Creates a new <see cref="DbDataMapper"/> with MySQL type mappers.
	/// </summary>
	public static DbDataMapper WithMySqlTypeMapperFactory(this DbDataMapper dataMapper) =>
		dataMapper.WithTypeMapperFactories([.. dataMapper.TypeMapperFactories, new MySqlDbTypeMapperFactory()]);
}
