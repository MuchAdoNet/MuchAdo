using System.Collections;
using MuchAdo.Mappers;

namespace MuchAdo;

public sealed class DefaultDbTypeMapperFactory : DbTypeMapperFactory
{
	public DefaultDbTypeMapperFactory(DefaultDbTypeMapperSettings? settings = null)
	{
		m_allowStringToEnum = settings?.AllowStringToEnum is true;
	}

	public override DbTypeMapper<T>? TryCreateTypeMapper<T>(DbDataMapper dataMapper)
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
		{
			if (m_allowStringToEnum)
				return (DbTypeMapper<T>) (Activator.CreateInstance(typeof(FlexibleEnumMapper<>).MakeGenericType(typeof(T)))!);

			var underlyingType = Enum.GetUnderlyingType(typeof(T));
			return (DbTypeMapper<T>) (Activator.CreateInstance(typeof(NumericEnumMapper<,>).MakeGenericType(typeof(T), underlyingType), dataMapper.GetTypeMapper(underlyingType))!);
		}

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
			return (DbTypeMapper<T>) (Activator.CreateInstance(typeof(NullableValueMapper<>).MakeGenericType(nonNullType), dataMapper.GetTypeMapper(nonNullType))!);

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
			return (DbTypeMapper<T>) Activator.CreateInstance(tupleMapperType.MakeGenericType(tupleTypes), [.. tupleTypes.Select(dataMapper.GetTypeMapper)])!;
		}

		return null;
	}

	private readonly bool m_allowStringToEnum;
}
