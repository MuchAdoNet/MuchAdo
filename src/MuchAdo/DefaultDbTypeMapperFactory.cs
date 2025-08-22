using System.Collections;
using MuchAdo.Mappers;

namespace MuchAdo;

internal sealed class DefaultDbTypeMapperFactory : DbTypeMapperFactory
{
	public override DbTypeMapper<T>? TryCreateTypeMapper<T>(DbDataMapper dataMapper)
	{
		if (typeof(T) == typeof(bool))
			return (DbTypeMapper<T>) (object) new BooleanMapper(dataMapper);
		if (typeof(T) == typeof(byte))
			return (DbTypeMapper<T>) (object) new ByteMapper(dataMapper);
		if (typeof(T) == typeof(char))
			return (DbTypeMapper<T>) (object) new CharMapper(dataMapper);
		if (typeof(T) == typeof(Guid))
			return (DbTypeMapper<T>) (object) new GuidMapper(dataMapper);
		if (typeof(T) == typeof(short))
			return (DbTypeMapper<T>) (object) new Int16Mapper(dataMapper);
		if (typeof(T) == typeof(int))
			return (DbTypeMapper<T>) (object) new Int32Mapper(dataMapper);
		if (typeof(T) == typeof(long))
			return (DbTypeMapper<T>) (object) new Int64Mapper(dataMapper);
		if (typeof(T) == typeof(float))
			return (DbTypeMapper<T>) (object) new FloatMapper(dataMapper);
		if (typeof(T) == typeof(double))
			return (DbTypeMapper<T>) (object) new DoubleMapper(dataMapper);
		if (typeof(T) == typeof(string))
			return (DbTypeMapper<T>) (object) new StringMapper(dataMapper);
		if (typeof(T) == typeof(decimal))
			return (DbTypeMapper<T>) (object) new DecimalMapper(dataMapper);
		if (typeof(T) == typeof(DateTime))
			return (DbTypeMapper<T>) (object) new DateTimeMapper(dataMapper);

		if (typeof(T) == typeof(DateTimeOffset))
			return (DbTypeMapper<T>) (object) new GetFieldValueMapper<DateTimeOffset>(dataMapper);
		if (typeof(T) == typeof(sbyte))
			return (DbTypeMapper<T>) (object) new GetFieldValueMapper<sbyte>(dataMapper);
		if (typeof(T) == typeof(ushort))
			return (DbTypeMapper<T>) (object) new GetFieldValueMapper<ushort>(dataMapper);
		if (typeof(T) == typeof(uint))
			return (DbTypeMapper<T>) (object) new GetFieldValueMapper<uint>(dataMapper);
		if (typeof(T) == typeof(ulong))
			return (DbTypeMapper<T>) (object) new GetFieldValueMapper<ulong>(dataMapper);
		if (typeof(T) == typeof(TimeSpan))
			return (DbTypeMapper<T>) (object) new GetFieldValueMapper<TimeSpan>(dataMapper);

#if NET
		if (typeof(T) == typeof(DateOnly))
			return (DbTypeMapper<T>) (object) new GetFieldValueMapper<DateOnly>(dataMapper);
		if (typeof(T) == typeof(TimeOnly))
			return (DbTypeMapper<T>) (object) new GetFieldValueMapper<TimeOnly>(dataMapper);
#endif

		if (typeof(T) == typeof(byte[]))
			return (DbTypeMapper<T>) (object) new ByteArrayMapper(dataMapper);

		if (typeof(T) == typeof(object))
			return (DbTypeMapper<T>) (object) new ObjectMapper(dataMapper);

		if (typeof(T).IsEnum)
		{
			if (dataMapper.AllowStringToEnum)
				return (DbTypeMapper<T>) (Activator.CreateInstance(typeof(FlexibleEnumMapper<>).MakeGenericType(typeof(T)), dataMapper)!);

			var underlyingType = Enum.GetUnderlyingType(typeof(T));
			return (DbTypeMapper<T>) (Activator.CreateInstance(typeof(NumericEnumMapper<,>).MakeGenericType(typeof(T), underlyingType), dataMapper, dataMapper.GetTypeMapper(underlyingType))!);
		}

		if (typeof(T).IsGenericType &&
			typeof(T).GetGenericTypeDefinition() is { } genericType &&
			(genericType == typeof(Dictionary<,>) ||
				genericType == typeof(IDictionary<,>) ||
				genericType == typeof(IReadOnlyDictionary<,>)) &&
			typeof(T).GetGenericArguments() is [var keyType, var valueType] &&
			keyType == typeof(string))
		{
			var dictionaryMapperType = typeof(DictionaryMapper<,>).MakeGenericType(typeof(T), valueType);
			return (DbTypeMapper<T>) Activator.CreateInstance(dictionaryMapperType, dataMapper)!;
		}

		if (typeof(T) == typeof(IDictionary))
			return (DbTypeMapper<T>) (object) new DictionaryMapper<IDictionary, object?>(dataMapper);

		if (typeof(T) == typeof(Stream))
			return (DbTypeMapper<T>) (object) new StreamMapper(dataMapper);
		if (typeof(T) == typeof(TextReader))
			return (DbTypeMapper<T>) (object) new TextReaderMapper(dataMapper);

		if (Nullable.GetUnderlyingType(typeof(T)) is { } nonNullType)
			return (DbTypeMapper<T>) Activator.CreateInstance(typeof(NullableValueMapper<>).MakeGenericType(nonNullType), dataMapper.GetTypeMapper(nonNullType))!;

		if (IsValueTupleType(typeof(T)))
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
			return (DbTypeMapper<T>) Activator.CreateInstance(tupleMapperType.MakeGenericType(tupleTypes), [dataMapper, .. tupleTypes.Select(dataMapper.GetTypeMapper)])!;
		}

		static bool IsValueTupleType(Type type)
		{
			if (!type.IsGenericType)
				return false;

			var genericType = type.GetGenericTypeDefinition();
			return genericType == typeof(ValueTuple<>) ||
				genericType == typeof(ValueTuple<,>) ||
				genericType == typeof(ValueTuple<,,>) ||
				genericType == typeof(ValueTuple<,,,>) ||
				genericType == typeof(ValueTuple<,,,,>) ||
				genericType == typeof(ValueTuple<,,,,,>) ||
				genericType == typeof(ValueTuple<,,,,,,>) ||
				genericType == typeof(ValueTuple<,,,,,,,>);
		}

		return null;
	}
}
