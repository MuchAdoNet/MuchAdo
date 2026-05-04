using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.ExceptionServices;
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
		=> (DbTypeMapper<T>) GetTypeMapper(typeof(T), CreateTypeMapper<T>);

	/// <summary>
	/// Gets a type mapper for the specified type.
	/// </summary>
	public DbTypeMapper GetTypeMapper(Type type)
		=> GetTypeMapper(type, () => CreateTypeMapper(type));

	private DbTypeMapper GetTypeMapper(Type type, Func<DbTypeMapper> createTypeMapper)
	{
		while (true)
		{
			if (m_typeMappers.TryGetValue(type, out var mapper))
			{
				if (mapper is not InProgressTypeMapper)
					return mapper;

				lock (m_typeMappersLock)
				{
					if (!m_typeMappers.TryGetValue(type, out mapper))
						continue;

					if (mapper is InProgressTypeMapper)
						throw CircularReference(type);

					return mapper;
				}
			}

			lock (m_typeMappersLock)
			{
				if (m_typeMappers.TryGetValue(type, out var existingMapper))
				{
					if (existingMapper is InProgressTypeMapper)
						throw CircularReference(type);

					return existingMapper;
				}

				m_typeMappers[type] = new InProgressTypeMapper(type);
				try
				{
					mapper = createTypeMapper();
				}
				catch
				{
					m_typeMappers.TryRemove(type, out _);
					throw;
				}
				m_typeMappers[type] = mapper;
				return mapper;
			}
		}
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

	private DbTypeMapper CreateTypeMapper(Type type)
	{
		try
		{
			return (DbTypeMapper) s_createTypeMapper.MakeGenericMethod(type).Invoke(this, [])!;
		}
		catch (TargetInvocationException exception) when (exception.InnerException is not null)
		{
			ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
			throw;
		}
	}

	private static InvalidOperationException CircularReference(Type type) =>
		new($"Circular reference detected while creating a type mapper for {type.FullName}.");

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
#if NET
	private readonly Lock m_typeMappersLock = new();
#else
	private readonly object m_typeMappersLock = new();
#endif

	private sealed class InProgressTypeMapper(Type type) : DbTypeMapper
	{
		public override Type Type => type;

		public override int? FieldCount => null;
	}
}
