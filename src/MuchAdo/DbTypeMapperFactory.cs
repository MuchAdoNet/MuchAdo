namespace MuchAdo;

public abstract class DbTypeMapperFactory
{
	public abstract DbTypeMapper<T>? TryCreateTypeMapper<T>(DbDataMapper dataMapper);
}
