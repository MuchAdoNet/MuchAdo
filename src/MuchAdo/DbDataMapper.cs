using System.Collections.Concurrent;
using System.Reflection;
using MuchAdo.Mappers;

namespace MuchAdo;

/// <summary>
/// Maps data record values to objects.
/// </summary>
public sealed class DbDataMapper
{
	/// <summary>
	/// The default data mapper.
	/// </summary>
	public static DbDataMapper Default { get; } = new();

	/// <summary>
	/// The type mapper factories used by this data mapper.
	/// </summary>
	/// <remarks>The default data mapper has a single type mapper factory that
	/// provides the default type mappers. Any type not handled by a type mapper
	/// factory is mapped as a DTO.</remarks>
	public IReadOnlyList<DbTypeMapperFactory> TypeMapperFactories { get; private init; }

	/// <summary>
	/// Returns a new data mapper with the specified type mapper factories.
	/// </summary>
	public DbDataMapper WithTypeMapperFactories(IReadOnlyList<DbTypeMapperFactory> value) =>
		new(this) { TypeMapperFactories = value };

	/// <summary>
	/// True to allow strings to be mapped to enums.
	/// </summary>
	public bool AllowStringToEnum { get; private init; }

	/// <summary>
	/// Returns a new data mapper with the specified value for <see cref="AllowStringToEnum"/>.
	/// </summary>
	public DbDataMapper WithAllowStringToEnum(bool value = true) =>
		new(this) { AllowStringToEnum = value };

	/// <summary>
	/// True to ignore unused fields.
	/// </summary>
	public bool IgnoreUnusedFields { get; private init; }

	/// <summary>
	/// Returns a new data mapper with the specified value for <see cref="IgnoreUnusedFields"/>.
	/// </summary>
	public DbDataMapper WithIgnoreUnusedFields(bool value = true) =>
		new(this) { IgnoreUnusedFields = value };

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
		foreach (var factory in TypeMapperFactories)
		{
			if (factory.TryCreateTypeMapper<T>(this) is { } mapper)
				return mapper;
		}

		return new DtoMapper<T>(this);
	}

	private DbDataMapper()
	{
		TypeMapperFactories = [new DefaultDbTypeMapperFactory()];
		AllowStringToEnum = false;
		IgnoreUnusedFields = false;
	}

	private DbDataMapper(DbDataMapper source)
	{
		TypeMapperFactories = source.TypeMapperFactories;
		AllowStringToEnum = source.AllowStringToEnum;
		IgnoreUnusedFields = source.IgnoreUnusedFields;
	}

	private static readonly MethodInfo s_createTypeMapper = typeof(DbDataMapper).GetMethod(nameof(CreateTypeMapper), BindingFlags.NonPublic | BindingFlags.Instance, null, [], null)!;

	private readonly ConcurrentDictionary<Type, DbTypeMapper> m_typeMappers = new();
}
