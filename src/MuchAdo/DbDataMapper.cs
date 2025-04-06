using System.Collections;
using System.Collections.Concurrent;
using System.Data;
using System.Data.Common;
using System.Dynamic;
using System.Linq.Expressions;
using System.Reflection;

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
	internal DbTypeMapper<T> GetTypeMapper<T>()
	{
		DbTypeMapper? mapper;
		while (!s_typeMappers.TryGetValue(typeof(T), out mapper))
			s_typeMappers.TryAdd(typeof(T), CreateTypeMapper<T>());
		return (DbTypeMapper<T>) mapper;
	}

	/// <summary>
	/// Gets a type mapper for the specified type.
	/// </summary>
	internal DbTypeMapper GetTypeMapper(Type type)
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

	private abstract class TypeMapper<T> : DbTypeMapper<T>
	{
		protected InvalidOperationException BadFieldCount(int count) => new($"{Type.FullName} must be read from {FieldCount} fields but is being read from {count} fields.");

		protected InvalidOperationException NotNullable() => new($"{Type.FullName} cannot be read from a null field.");

		protected InvalidOperationException BadCast(Type? type, Exception exception) => new($"Failed to cast {type?.FullName} to {Type.FullName}.", exception);
	}

	private sealed class DtoMapper<T> : TypeMapper<T>
	{
		public DtoMapper(DbDataMapper mapper)
		{
			var properties = DbDtoInfo.GetInfo<T>().Properties;

			var propertiesByNormalizedFieldName = new Dictionary<string, (DbDtoProperty<T> Property, DbTypeMapper Mapper)>(capacity: properties.Count, StringComparer.OrdinalIgnoreCase);
			foreach (var property in properties)
				propertiesByNormalizedFieldName.Add(NormalizeFieldName(property.ColumnName ?? property.Name), (property, mapper.GetTypeMapper(property.ValueType)));
			m_propertiesByNormalizedFieldName = propertiesByNormalizedFieldName;
		}

		public override int? FieldCount => null;

		protected override T MapCore(IDataRecord record, int index, int count, DbRecordState? state)
		{
			if (IsAllNull(record, index, count))
				return default!;

			if (state?.Get(this, index, count) is not Func<IDataRecord, int, DbRecordState?, T> func)
			{
				var fieldNames = new string[count];
				for (var i = 0; i < count; i++)
					fieldNames[i] = record.GetName(index + i);
				func = m_funcsByFieldNameSet.GetOrAdd(new FieldNameSet(fieldNames), CreateFunc);
				state?.Set(this, index, count, func);
			}

			return func(record, index, state);
		}

		private static bool IsAllNull(IDataRecord record, int index, int count)
		{
			for (var i = 0; i < count; i++)
			{
				if (!record.IsDBNull(index + i))
					return false;
			}
			return true;
		}

		private Func<IDataRecord, int, DbRecordState?, T> CreateFunc(FieldNameSet fieldNameSet)
		{
			foreach (var creator in DbDtoInfo.GetInfo<T>().Creators)
			{
				var count = fieldNameSet.Names.Count;
				var constructorParameters = creator is null ? null : new Expression?[creator.Parameters.Length];
				var memberBindings = new List<MemberBinding>(capacity: count);

				var canCreate = true;
				for (var index = 0; index < count; index++)
				{
					var fieldName = fieldNameSet.Names[index];
					if (!m_propertiesByNormalizedFieldName!.TryGetValue(NormalizeFieldName(fieldName), out var property))
						throw new InvalidOperationException($"Type does not have a property for '{fieldName}': {Type.FullName}");

					if (creator?.GetPropertyParameterIndex(property.Property) is { } parameterIndex)
					{
						constructorParameters![parameterIndex] =
							Expression.Call(
								Expression.Constant(property.Mapper),
								property.Mapper.GetType().GetMethod("Map", [typeof(IDataRecord), typeof(int), typeof(int), typeof(DbRecordState)])!,
								s_recordParam,
								Expression.Add(s_indexParam, Expression.Constant(index)),
								Expression.Constant(1),
								s_stateParam);
					}
					else
					{
						if (property.Property.IsReadOnly)
						{
							canCreate = false;
							break;
						}

						memberBindings.Add(
							Expression.Bind(
								property.Property.MemberInfo,
								Expression.Call(
									Expression.Constant(property.Mapper),
									property.Mapper.GetType().GetMethod("Map", [typeof(IDataRecord), typeof(int), typeof(int), typeof(DbRecordState)])!,
									s_recordParam,
									Expression.Add(s_indexParam, Expression.Constant(index)),
									Expression.Constant(1),
									s_stateParam)));
					}
				}
				if (!canCreate)
					continue;

				if (creator is not null)
				{
					for (var index = 0; index < constructorParameters!.Length; index++)
					{
						constructorParameters[index] ??= creator.DefaultValues?[index] is { } defaultValue
							? Expression.Constant(defaultValue)
							: Expression.Default(creator.Parameters[index].ValueType);
					}
				}

				var newExpression = creator is not null
					? Expression.New(creator.Constructor, constructorParameters!)
					: Expression.New(typeof(T));
				var initExpression = memberBindings.Count != 0
					? Expression.MemberInit(newExpression, memberBindings)
					: (Expression) newExpression;
				return (Func<IDataRecord, int, DbRecordState?, T>)
					Expression.Lambda(initExpression, s_recordParam, s_indexParam, s_stateParam).Compile();
			}

			throw new InvalidOperationException($"DTO {typeof(T).FullName} could not be created from fields: {string.Join(", ", fieldNameSet.Names)}");
		}

		private static string NormalizeFieldName(string text) => text.ReplaceOrdinal("_", "");

		private readonly IReadOnlyDictionary<string, (DbDtoProperty<T> Property, DbTypeMapper Mapper)>? m_propertiesByNormalizedFieldName;

		private sealed class FieldNameSet(IReadOnlyList<string> names) : IEquatable<FieldNameSet>
		{
			public IReadOnlyList<string> Names { get; } = names;

			public bool Equals(FieldNameSet? other) => other is not null && Names.SequenceEqual(other.Names, StringComparer.OrdinalIgnoreCase);

			public override bool Equals(object? obj) => obj is FieldNameSet other && Equals(other);

			public override int GetHashCode() => Names.Aggregate(0, (hash, name) => PortableUtility.CombineHashCodes(hash, StringComparer.OrdinalIgnoreCase.GetHashCode(name)));
		}

		private readonly ConcurrentDictionary<FieldNameSet, Func<IDataRecord, int, DbRecordState?, T>> m_funcsByFieldNameSet = new();
	}

	private sealed class ObjectMapper : TypeMapper<object>
	{
		public override int? FieldCount => null;

		protected override object MapCore(IDataRecord record, int index, int count, DbRecordState? state)
		{
			if (count == 1)
			{
				var value = record.GetValue(index);
				return value == DBNull.Value ? null! : value;
			}
			else
			{
				IDictionary<string, object?> obj = new ExpandoObject();
				var notNull = false;
				for (var i = index; i < index + count; i++)
				{
					var name = record.GetName(i);
					if (!record.IsDBNull(i))
					{
						obj[name] = record.GetValue(i);
						notNull = true;
					}
					else
					{
						obj[name] = null;
					}
				}
				return notNull ? obj : null!;
			}
		}
	}

	private sealed class DictionaryMapper<T> : TypeMapper<T>
	{
		public override int? FieldCount => null;

		protected override T MapCore(IDataRecord record, int index, int count, DbRecordState? state)
		{
			var dictionary = new Dictionary<string, object?>();
			var notNull = false;
			for (var i = index; i < index + count; i++)
			{
				var name = record.GetName(i);
				if (!record.IsDBNull(i))
				{
					dictionary[name] = record.GetValue(i);
					notNull = true;
				}
				else
				{
					dictionary[name] = null;
				}
			}
			return notNull ? (T) (object) dictionary : default!;
		}
	}

	private abstract class ValueTupleMapperBase<T>(DbTypeMapper[] mappers) : TypeMapper<T>
	{
		public override int? FieldCount
		{
			get
			{
				var totalFieldCount = 0;
				foreach (var mapper in mappers)
				{
					if (mapper.FieldCount is not { } fieldCount)
						return null;
					totalFieldCount += fieldCount;
				}
				return totalFieldCount;
			}
		}

		protected void GetValueRanges(IDataRecord record, int index, int count, Span<(int Index, int Count)> valueRanges)
		{
			if (FieldCount is { } requiredFieldCount && count != requiredFieldCount)
				throw BadFieldCount(count);

			var valueCount = mappers.Length;
			var recordIndex = index;
			for (var valueIndex = 0; valueIndex < valueCount; valueIndex++)
			{
				var mapperFieldCount = mappers[valueIndex].FieldCount;

				int fieldCount;
				int? nullIndex = null;
				if (mapperFieldCount is null)
				{
					int? remainingFieldCount = 0;
					var minimumRemainingFieldCount = 0;
					for (var nextValueIndex = valueIndex + 1; nextValueIndex < valueCount; nextValueIndex++)
					{
						var nextFieldCount = mappers[nextValueIndex].FieldCount;
						if (nextFieldCount is not null)
						{
							remainingFieldCount += nextFieldCount.Value;
							minimumRemainingFieldCount += nextFieldCount.Value;
						}
						else
						{
							remainingFieldCount = null;
							minimumRemainingFieldCount += 1;
						}
					}

					if (remainingFieldCount is not null)
					{
						fieldCount = count - recordIndex - remainingFieldCount.Value;
					}
					else
					{
						for (var nextRecordIndex = recordIndex + 1; nextRecordIndex < count; nextRecordIndex++)
						{
							if (record.GetName(nextRecordIndex).Equals("NULL", StringComparison.OrdinalIgnoreCase))
							{
								nullIndex = nextRecordIndex;
								break;
							}
						}

						if (nullIndex is not null)
						{
							fieldCount = nullIndex.Value - recordIndex;
						}
						else if (count - (recordIndex + 1) == minimumRemainingFieldCount)
						{
							fieldCount = 1;
						}
						else
						{
							throw new InvalidOperationException($"Tuple item {valueIndex} must be terminated by a field named 'NULL': {Type.FullName}");
						}
					}
				}
				else
				{
					fieldCount = mapperFieldCount.Value;
				}

				valueRanges[valueIndex] = (recordIndex, fieldCount);
				recordIndex = nullIndex + 1 ?? recordIndex + fieldCount;
			}
		}
	}

	private sealed class ValueTupleMapper<T1>(DbTypeMapper<T1> mapper1)
		: ValueTupleMapperBase<ValueTuple<T1>>([mapper1])
	{
		protected override ValueTuple<T1> MapCore(IDataRecord record, int index, int count, DbRecordState? state)
		{
			Span<(int Index, int Count)> valueRanges = stackalloc (int Index, int Count)[1];
			GetValueRanges(record, index, count, valueRanges);
			return new ValueTuple<T1>(mapper1.Map(record, valueRanges[0].Index, valueRanges[0].Count, state));
		}
	}

	private sealed class ValueTupleMapper<T1, T2>(DbTypeMapper<T1> mapper1, DbTypeMapper<T2> mapper2)
		: ValueTupleMapperBase<(T1, T2)>([mapper1, mapper2])
	{
		protected override (T1, T2) MapCore(IDataRecord record, int index, int count, DbRecordState? state)
		{
			Span<(int Index, int Count)> valueRanges = stackalloc (int Index, int Count)[2];
			GetValueRanges(record, index, count, valueRanges);
			return (
				mapper1.Map(record, valueRanges[0].Index, valueRanges[0].Count, state),
				mapper2.Map(record, valueRanges[1].Index, valueRanges[1].Count, state));
		}
	}

	private sealed class ValueTupleMapper<T1, T2, T3>(DbTypeMapper<T1> mapper1, DbTypeMapper<T2> mapper2, DbTypeMapper<T3> mapper3)
		: ValueTupleMapperBase<(T1, T2, T3)>([mapper1, mapper2, mapper3])
	{
		protected override (T1, T2, T3) MapCore(IDataRecord record, int index, int count, DbRecordState? state)
		{
			Span<(int Index, int Count)> valueRanges = stackalloc (int Index, int Count)[3];
			GetValueRanges(record, index, count, valueRanges);
			return (
				mapper1.Map(record, valueRanges[0].Index, valueRanges[0].Count, state),
				mapper2.Map(record, valueRanges[1].Index, valueRanges[1].Count, state),
				mapper3.Map(record, valueRanges[2].Index, valueRanges[2].Count, state));
		}
	}

	private sealed class ValueTupleMapper<T1, T2, T3, T4>(DbTypeMapper<T1> mapper1, DbTypeMapper<T2> mapper2, DbTypeMapper<T3> mapper3, DbTypeMapper<T4> mapper4)
		: ValueTupleMapperBase<(T1, T2, T3, T4)>([mapper1, mapper2, mapper3, mapper4])
	{
		protected override (T1, T2, T3, T4) MapCore(IDataRecord record, int index, int count, DbRecordState? state)
		{
			Span<(int Index, int Count)> valueRanges = stackalloc (int Index, int Count)[4];
			GetValueRanges(record, index, count, valueRanges);
			return (
				mapper1.Map(record, valueRanges[0].Index, valueRanges[0].Count, state),
				mapper2.Map(record, valueRanges[1].Index, valueRanges[1].Count, state),
				mapper3.Map(record, valueRanges[2].Index, valueRanges[2].Count, state),
				mapper4.Map(record, valueRanges[3].Index, valueRanges[3].Count, state));
		}
	}

	private sealed class ValueTupleMapper<T1, T2, T3, T4, T5>(DbTypeMapper<T1> mapper1, DbTypeMapper<T2> mapper2, DbTypeMapper<T3> mapper3, DbTypeMapper<T4> mapper4, DbTypeMapper<T5> mapper5)
		: ValueTupleMapperBase<(T1, T2, T3, T4, T5)>([mapper1, mapper2, mapper3, mapper4, mapper5])
	{
		protected override (T1, T2, T3, T4, T5) MapCore(IDataRecord record, int index, int count, DbRecordState? state)
		{
			Span<(int Index, int Count)> valueRanges = stackalloc (int Index, int Count)[5];
			GetValueRanges(record, index, count, valueRanges);
			return (
				mapper1.Map(record, valueRanges[0].Index, valueRanges[0].Count, state),
				mapper2.Map(record, valueRanges[1].Index, valueRanges[1].Count, state),
				mapper3.Map(record, valueRanges[2].Index, valueRanges[2].Count, state),
				mapper4.Map(record, valueRanges[3].Index, valueRanges[3].Count, state),
				mapper5.Map(record, valueRanges[4].Index, valueRanges[4].Count, state));
		}
	}

	private sealed class ValueTupleMapper<T1, T2, T3, T4, T5, T6>(DbTypeMapper<T1> mapper1, DbTypeMapper<T2> mapper2, DbTypeMapper<T3> mapper3, DbTypeMapper<T4> mapper4, DbTypeMapper<T5> mapper5, DbTypeMapper<T6> mapper6)
		: ValueTupleMapperBase<(T1, T2, T3, T4, T5, T6)>([mapper1, mapper2, mapper3, mapper4, mapper5, mapper6])
	{
		protected override (T1, T2, T3, T4, T5, T6) MapCore(IDataRecord record, int index, int count, DbRecordState? state)
		{
			Span<(int Index, int Count)> valueRanges = stackalloc (int Index, int Count)[6];
			GetValueRanges(record, index, count, valueRanges);
			return (
				mapper1.Map(record, valueRanges[0].Index, valueRanges[0].Count, state),
				mapper2.Map(record, valueRanges[1].Index, valueRanges[1].Count, state),
				mapper3.Map(record, valueRanges[2].Index, valueRanges[2].Count, state),
				mapper4.Map(record, valueRanges[3].Index, valueRanges[3].Count, state),
				mapper5.Map(record, valueRanges[4].Index, valueRanges[4].Count, state),
				mapper6.Map(record, valueRanges[5].Index, valueRanges[5].Count, state));
		}
	}

	private sealed class ValueTupleMapper<T1, T2, T3, T4, T5, T6, T7>(DbTypeMapper<T1> mapper1, DbTypeMapper<T2> mapper2, DbTypeMapper<T3> mapper3, DbTypeMapper<T4> mapper4, DbTypeMapper<T5> mapper5, DbTypeMapper<T6> mapper6, DbTypeMapper<T7> mapper7)
		: ValueTupleMapperBase<(T1, T2, T3, T4, T5, T6, T7)>([mapper1, mapper2, mapper3, mapper4, mapper5, mapper6, mapper7])
	{
		protected override (T1, T2, T3, T4, T5, T6, T7) MapCore(IDataRecord record, int index, int count, DbRecordState? state)
		{
			Span<(int Index, int Count)> valueRanges = stackalloc (int Index, int Count)[7];
			GetValueRanges(record, index, count, valueRanges);
			return (
				mapper1.Map(record, valueRanges[0].Index, valueRanges[0].Count, state),
				mapper2.Map(record, valueRanges[1].Index, valueRanges[1].Count, state),
				mapper3.Map(record, valueRanges[2].Index, valueRanges[2].Count, state),
				mapper4.Map(record, valueRanges[3].Index, valueRanges[3].Count, state),
				mapper5.Map(record, valueRanges[4].Index, valueRanges[4].Count, state),
				mapper6.Map(record, valueRanges[5].Index, valueRanges[5].Count, state),
				mapper7.Map(record, valueRanges[6].Index, valueRanges[6].Count, state));
		}
	}

	private sealed class ValueTupleMapperRest<T1, T2, T3, T4, T5, T6, T7, TRest>(DbTypeMapper<T1> mapper1, DbTypeMapper<T2> mapper2, DbTypeMapper<T3> mapper3, DbTypeMapper<T4> mapper4, DbTypeMapper<T5> mapper5, DbTypeMapper<T6> mapper6, DbTypeMapper<T7> mapper7, DbTypeMapper<TRest> mapperRest)
		: ValueTupleMapperBase<ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest>>([mapper1, mapper2, mapper3, mapper4, mapper5, mapper6, mapper7, mapperRest])
		where TRest : struct
	{
		protected override ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest> MapCore(IDataRecord record, int index, int count, DbRecordState? state)
		{
			Span<(int Index, int Count)> valueRanges = stackalloc (int Index, int Count)[8];
			GetValueRanges(record, index, count, valueRanges);
			return new ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest>(
				mapper1.Map(record, valueRanges[0].Index, valueRanges[0].Count, state),
				mapper2.Map(record, valueRanges[1].Index, valueRanges[1].Count, state),
				mapper3.Map(record, valueRanges[2].Index, valueRanges[2].Count, state),
				mapper4.Map(record, valueRanges[3].Index, valueRanges[3].Count, state),
				mapper5.Map(record, valueRanges[4].Index, valueRanges[4].Count, state),
				mapper6.Map(record, valueRanges[5].Index, valueRanges[5].Count, state),
				mapper7.Map(record, valueRanges[6].Index, valueRanges[6].Count, state),
				mapperRest.Map(record, valueRanges[7].Index, valueRanges[7].Count, state));
		}
	}

	private abstract class SingleFieldMapper<T> : TypeMapper<T>
	{
		public override int? FieldCount => 1;

		protected sealed override T MapCore(IDataRecord record, int index, int count, DbRecordState? state) =>
			count == 1 ? MapField(record, index) : throw BadFieldCount(count);

		protected abstract T MapField(IDataRecord record, int index);
	}

	private abstract class NonNullableValueMapper<T> : SingleFieldMapper<T>
		where T : struct
	{
		protected sealed override T MapField(IDataRecord record, int index) =>
			!record.IsDBNull(index) ? MapNotNullField(record, index) : throw NotNullable();

		public abstract T MapNotNullField(IDataRecord record, int index);
	}

	private sealed class NullableValueMapper<T>(NonNullableValueMapper<T> mapper) : SingleFieldMapper<T?>
		where T : struct
	{
		protected override T? MapField(IDataRecord record, int index) =>
			!record.IsDBNull(index) ? mapper.MapNotNullField(record, index) : null;
	}

	private abstract class ReferenceValueMapper<T> : SingleFieldMapper<T?>
		where T : class
	{
		protected sealed override T? MapField(IDataRecord record, int index) =>
			!record.IsDBNull(index) ? MapNotNullField(record, index) : null;

		public abstract T MapNotNullField(IDataRecord record, int index);
	}

	private sealed class StringMapper : ReferenceValueMapper<string>
	{
		public override string MapNotNullField(IDataRecord record, int index) => record.GetString(index);
	}

	private sealed class BooleanMapper : NonNullableValueMapper<bool>
	{
		public override bool MapNotNullField(IDataRecord record, int index) => record.GetBoolean(index);
	}

	private sealed class ByteMapper : NonNullableValueMapper<byte>
	{
		public override byte MapNotNullField(IDataRecord record, int index) => record.GetByte(index);
	}

	private sealed class CharMapper : NonNullableValueMapper<char>
	{
		public override char MapNotNullField(IDataRecord record, int index) => record.GetChar(index);
	}

	private sealed class GuidMapper : NonNullableValueMapper<Guid>
	{
		public override Guid MapNotNullField(IDataRecord record, int index) => record.GetGuid(index);
	}

	private sealed class Int16Mapper : NonNullableValueMapper<short>
	{
		public override short MapNotNullField(IDataRecord record, int index) => record.GetInt16(index);
	}

	private sealed class Int32Mapper : NonNullableValueMapper<int>
	{
		public override int MapNotNullField(IDataRecord record, int index) => record.GetInt32(index);
	}

	private sealed class Int64Mapper : NonNullableValueMapper<long>
	{
		public override long MapNotNullField(IDataRecord record, int index) => record.GetInt64(index);
	}

	private sealed class FloatMapper : NonNullableValueMapper<float>
	{
		public override float MapNotNullField(IDataRecord record, int index) => record.GetFloat(index);
	}

	private sealed class DoubleMapper : NonNullableValueMapper<double>
	{
		public override double MapNotNullField(IDataRecord record, int index) => record.GetDouble(index);
	}

	private sealed class DecimalMapper : NonNullableValueMapper<decimal>
	{
		public override decimal MapNotNullField(IDataRecord record, int index) => record.GetDecimal(index);
	}

	private sealed class DateTimeMapper : NonNullableValueMapper<DateTime>
	{
		public override DateTime MapNotNullField(IDataRecord record, int index) => record.GetDateTime(index);
	}

	private sealed class EnumMapper<T> : NonNullableValueMapper<T>
		where T : struct
	{
		public override T MapNotNullField(IDataRecord record, int index)
		{
			var value = record.GetValue(index);
			try
			{
				return value switch
				{
					T enumValue => enumValue,
#if !NETSTANDARD2_0
					string stringValue => Enum.Parse<T>(stringValue, ignoreCase: true),
#else
					string stringValue => (T) Enum.Parse(typeof(T), stringValue, ignoreCase: true),
#endif
					_ => (T) Enum.ToObject(typeof(T), value),
				};
			}
			catch (Exception exception) when (exception is ArgumentException or InvalidCastException)
			{
				throw BadCast(value.GetType(), exception);
			}
		}
	}

	private sealed class ByteArrayMapper : ReferenceValueMapper<byte[]>
	{
		public override byte[] MapNotNullField(IDataRecord record, int index)
		{
			if (record.GetValue(index) is byte[] blob)
				return blob;

			var byteCount = (int) record.GetBytes(index, fieldOffset: 0, buffer: null, bufferoffset: 0, length: 0);
			var bytes = new byte[byteCount];
			record.GetBytes(index, fieldOffset: 0, buffer: bytes, bufferoffset: 0, length: byteCount);
			return bytes;
		}
	}

	private sealed class StreamMapper : ReferenceValueMapper<Stream>
	{
		public override Stream MapNotNullField(IDataRecord record, int index)
		{
			if (record is DbDataReader dbReader)
				return dbReader.GetStream(index);

			var byteCount = (int) record.GetBytes(index, fieldOffset: 0, buffer: null, bufferoffset: 0, length: 0);
			var bytes = new byte[byteCount];
			record.GetBytes(index, fieldOffset: 0, buffer: bytes, bufferoffset: 0, length: byteCount);
			return new MemoryStream(buffer: bytes, index: 0, count: byteCount, writable: false);
		}
	}

	private static readonly ParameterExpression s_recordParam = Expression.Parameter(typeof(IDataRecord), "record");
	private static readonly ParameterExpression s_indexParam = Expression.Parameter(typeof(int), "index");
	private static readonly ParameterExpression s_stateParam = Expression.Parameter(typeof(DbRecordState), "state");

	private static readonly ConcurrentDictionary<Type, DbTypeMapper> s_typeMappers = new();
	private static readonly MethodInfo s_createTypeMapper = typeof(DbDataMapper).GetMethod(nameof(CreateTypeMapper), BindingFlags.NonPublic | BindingFlags.Instance, null, [], null)!;
}
