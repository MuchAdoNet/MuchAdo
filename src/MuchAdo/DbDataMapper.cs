using System.Collections;
using System.Collections.Concurrent;
using System.Data;
using System.Linq.Expressions;
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
	public static DbDataMapper Default { get; } = new();

	/// <summary>
	/// Gets a type mapper for the specified type.
	/// </summary>
	public DbTypeMapper<T> GetTypeMapper<T>()
	{
		DbTypeMapper? mapper;
		while (!s_typeMappers.TryGetValue(typeof(T), out mapper))
			s_typeMappers.TryAdd(typeof(T), CreateTypeMapper<T>());
		return (DbTypeMapper<T>) mapper;
	}

	/// <summary>
	/// Gets a type mapper for the specified type.
	/// </summary>
	public DbTypeMapper GetTypeMapper(Type type)
	{
		DbTypeMapper? mapper;
		while (!s_typeMappers.TryGetValue(type, out mapper))
			s_typeMappers.TryAdd(type, (DbTypeMapper) s_createTypeMapper.MakeGenericMethod(type).Invoke(this, [])!);
		return mapper;
	}

	protected virtual DbTypeMapper<T>? TryCreateTypeMapper<T>()
	{
		if (typeof(T) == typeof(string))
			return (DbTypeMapper<T>) (object) new StringMapper();

		if (typeof(T) == typeof(bool))
			return (DbTypeMapper<T>) (object) new BooleanMapper();

		if (typeof(T) == typeof(byte))
			return (DbTypeMapper<T>) (object) new ByteMapper();

		if (typeof(T) == typeof(char))
			return (DbTypeMapper<T>) (object) new CharMapper();

		if (typeof(T) == typeof(Guid))
			return (DbTypeMapper<T>) (object) new GuidMapper();

		if (typeof(T) == typeof(short))
			return (DbTypeMapper<T>) (object) new Int16Mapper();

		if (typeof(T) == typeof(int))
			return (DbTypeMapper<T>) (object) new Int32Mapper();

		if (typeof(T) == typeof(long))
			return (DbTypeMapper<T>) (object) new Int64Mapper();

		if (typeof(T) == typeof(float))
			return (DbTypeMapper<T>) (object) new FloatMapper();

		if (typeof(T) == typeof(double))
			return (DbTypeMapper<T>) (object) new DoubleMapper();

		if (typeof(T) == typeof(decimal))
			return (DbTypeMapper<T>) (object) new DecimalMapper();

		if (typeof(T) == typeof(DateTime))
			return (DbTypeMapper<T>) (object) new DateTimeMapper();

		if (typeof(T) == typeof(byte[]))
			return (DbTypeMapper<T>) (object) new ByteArrayMapper();

		if (typeof(T) == typeof(object))
			return (DbTypeMapper<T>) (object) new ObjectMapper();

		if (typeof(T).IsEnum)
			return (DbTypeMapper<T>) (Activator.CreateInstance(typeof(EnumMapper<>).MakeGenericType(typeof(T)), [])!);

		if (typeof(T) == typeof(Dictionary<string, object?>))
			return (DbTypeMapper<T>) (object) new DictionaryMapper<Dictionary<string, object?>>();
		if (typeof(T) == typeof(IDictionary<string, object?>))
			return (DbTypeMapper<T>) (object) new DictionaryMapper<IDictionary<string, object?>>();
		if (typeof(T) == typeof(IReadOnlyDictionary<string, object?>))
			return (DbTypeMapper<T>) (object) new DictionaryMapper<IReadOnlyDictionary<string, object?>>();
		if (typeof(T) == typeof(IDictionary))
			return (DbTypeMapper<T>) (object) new DictionaryMapper<IDictionary>();

		if (typeof(T) == typeof(Stream))
			return (DbTypeMapper<T>) (object) new StreamMapper();

		if (Nullable.GetUnderlyingType(typeof(T)) is { } nonNullType)
			return (DbTypeMapper<T>) (Activator.CreateInstance(typeof(NullableValueMapper<>).MakeGenericType(nonNullType), [GetTypeMapper(nonNullType)])!);

		var typeName = typeof(T).FullName ?? "";
		if (typeName.StartsWith("System.ValueTuple`", StringComparison.Ordinal))
		{
			var tupleTypes = typeof(T).GetGenericArguments();
			var tupleMapperType = tupleTypes.Length switch
			{
				1 => typeof(ValueTupleMapper<>),
				2 => typeof(ValueTupleMapper<,>),
				3 => typeof(ValueTupleMapper<,,>),
				4 => typeof(ValueTupleMapper<,,,>),
				5 => typeof(ValueTupleMapper<,,,,>),
				6 => typeof(ValueTupleMapper<,,,,,>),
				7 => typeof(ValueTupleMapper<,,,,,,>),
				_ => typeof(ValueTupleMapperRest<,,,,,,,>),
			};
			return (DbTypeMapper<T>) Activator.CreateInstance(tupleMapperType.MakeGenericType(tupleTypes), [.. tupleTypes.Select(GetTypeMapper)])!;
		}

		return null;
	}

	private DbTypeMapper<T> CreateTypeMapper<T>() => TryCreateTypeMapper<T>() ?? new DtoMapper<T>(this);

	internal static readonly ParameterExpression RecordParam = Expression.Parameter(typeof(IDataRecord), "record");
	internal static readonly ParameterExpression IndexParam = Expression.Parameter(typeof(int), "index");
	internal static readonly ParameterExpression StateParam = Expression.Parameter(typeof(DbConnectorRecordState), "state");

	private static readonly ConcurrentDictionary<Type, DbTypeMapper> s_typeMappers = new();
	private static readonly MethodInfo s_createTypeMapper = typeof(DbDataMapper).GetMethod(nameof(CreateTypeMapper), BindingFlags.NonPublic | BindingFlags.Instance, null, [], null)!;
}
