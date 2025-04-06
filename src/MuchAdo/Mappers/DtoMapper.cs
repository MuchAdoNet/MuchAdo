using System.Collections.Concurrent;
using System.Data;
using System.Linq.Expressions;

namespace MuchAdo.Mappers;

internal sealed class DtoMapper<T> : DbTypeMapper<T>
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

	protected override T MapCore(IDataRecord record, int index, int count, DbConnectorRecordState? state)
	{
		if (IsAllNull(record, index, count))
			return default!;

		if (state?.Get(this, index, count) is not Func<IDataRecord, int, DbConnectorRecordState?, T> func)
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

	private Func<IDataRecord, int, DbConnectorRecordState?, T> CreateFunc(FieldNameSet fieldNameSet)
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
							property.Mapper.GetType().GetMethod("Map", [typeof(IDataRecord), typeof(int), typeof(int), typeof(DbConnectorRecordState)])!,
							DbDataMapper.RecordParam,
							Expression.Add(DbDataMapper.IndexParam, Expression.Constant(index)),
							Expression.Constant(1),
							DbDataMapper.StateParam);
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
								property.Mapper.GetType().GetMethod("Map", [typeof(IDataRecord), typeof(int), typeof(int), typeof(DbConnectorRecordState)])!,
								DbDataMapper.RecordParam,
								Expression.Add(DbDataMapper.IndexParam, Expression.Constant(index)),
								Expression.Constant(1),
								DbDataMapper.StateParam)));
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
			return (Func<IDataRecord, int, DbConnectorRecordState?, T>)
				Expression.Lambda(initExpression, DbDataMapper.RecordParam, DbDataMapper.IndexParam, DbDataMapper.StateParam).Compile();
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

	private readonly ConcurrentDictionary<FieldNameSet, Func<IDataRecord, int, DbConnectorRecordState?, T>> m_funcsByFieldNameSet = new();
}
