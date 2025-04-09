using System.Collections.Concurrent;
using System.Reflection;
using MuchAdo.Mappers;

namespace MuchAdo;

/// <summary>
/// Maps data record values to objects.
/// </summary>
public class DbDataMapper
{
	/// <summary>
	/// The default data mapper allows the ADO.NET provider to convert values to the expected type.
	/// </summary>
	public static DbDataMapper Default { get; } = new(new DefaultDbTypeMapperFactory());

	/// <summary>
	/// Creates a new data mapper.
	/// </summary>
	public DbDataMapper(params IEnumerable<DbTypeMapperFactory> factories)
	{
		Factories = factories.ToList().AsReadOnly();
	}

	/// <summary>
	/// The type mapper factories used by this data mapper.
	/// </summary>
	public IReadOnlyList<DbTypeMapperFactory> Factories { get; }

	/// <summary>
	/// Gets a type mapper for the specified type.
	/// </summary>
	public DbTypeMapper<T> GetTypeMapper<T>()
	{
		DbTypeMapper? mapper;
		while (!m_typeMappers.TryGetValue(typeof(T), out mapper))
			m_typeMappers.TryAdd(typeof(T), CreateTypeMapper<T>());
		return (DbTypeMapper<T>) mapper;
	}

	/// <summary>
	/// Gets a type mapper for the specified type.
	/// </summary>
	public DbTypeMapper GetTypeMapper(Type type)
	{
		DbTypeMapper? mapper;
		while (!m_typeMappers.TryGetValue(type, out mapper))
			m_typeMappers.TryAdd(type, (DbTypeMapper) s_createTypeMapper.MakeGenericMethod(type).Invoke(this, [])!);
		return mapper;
	}

	private DbTypeMapper<T> CreateTypeMapper<T>()
	{
		foreach (var factory in Factories)
		{
			if (factory.TryCreateTypeMapper<T>(this) is { } mapper)
				return mapper;
		}

		return new DtoMapper<T>(this);
	}

	private static readonly MethodInfo s_createTypeMapper = typeof(DbDataMapper).GetMethod(nameof(CreateTypeMapper), BindingFlags.NonPublic | BindingFlags.Instance, null, [], null)!;

	private readonly ConcurrentDictionary<Type, DbTypeMapper> m_typeMappers = new();
}
