using System.Collections.Concurrent;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

namespace MuchAdo.Mappers;

internal static class DtoMapper
{
	internal static readonly ParameterExpression RecordParam = Expression.Parameter(typeof(IDataRecord), "record");
	internal static readonly ParameterExpression IndexParam = Expression.Parameter(typeof(int), "index");
	internal static readonly ParameterExpression StateParam = Expression.Parameter(typeof(DbConnectorRecordState), "state");

	internal sealed class FieldNameSet(IReadOnlyList<string> names) : IEquatable<FieldNameSet>
	{
		public IReadOnlyList<string> Names { get; } = names;

		public bool Equals(FieldNameSet? other) => other is not null && Names.SequenceEqual(other.Names, StringComparer.OrdinalIgnoreCase);

		public override bool Equals(object? obj) => obj is FieldNameSet other && Equals(other);

		public override int GetHashCode() => Names.Aggregate(0, (hash, name) => Utility.CombineHashCodes(hash, StringComparer.OrdinalIgnoreCase.GetHashCode(name)));
	}
}

[SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "Same name.")]
internal sealed class DtoMapper<T> : DbTypeMapper<T>
{
	public DtoMapper(DbDataMapper dataMapper)
	{
		m_dataMapper = dataMapper;
		var properties = DbDtoInfo.GetInfo<T>().Properties;
		var propertiesByNormalizedFieldName = new Dictionary<string, (DbDtoProperty<T> Property, DbTypeMapper Mapper)>(capacity: properties.Count, StringComparer.OrdinalIgnoreCase);
		foreach (var property in properties)
			propertiesByNormalizedFieldName.Add(NormalizeFieldName(property.ColumnName ?? property.Name), (property, m_dataMapper.GetTypeMapper(property.ValueType)));
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
			func = m_funcsByFieldNameSet.GetOrAdd(new DtoMapper.FieldNameSet(fieldNames), CreateFunc);
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

	private Func<IDataRecord, int, DbConnectorRecordState?, T> CreateFunc(DtoMapper.FieldNameSet fieldNameSet)
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
				{
					if (m_dataMapper.IgnoreUnusedFields)
						continue;
					else
						throw new InvalidOperationException($"Type does not have a property for '{fieldName}': {Type.FullName}");
				}

				if (creator?.GetPropertyParameterIndex(property.Property) is { } parameterIndex)
				{
					constructorParameters![parameterIndex] =
						Expression.Call(
							Expression.Constant(property.Mapper),
							property.Mapper.GetType().GetMethod("Map", [typeof(IDataRecord), typeof(int), typeof(int), typeof(DbConnectorRecordState)])!,
							DtoMapper.RecordParam,
							Expression.Add(DtoMapper.IndexParam, Expression.Constant(index)),
							Expression.Constant(1),
							DtoMapper.StateParam);
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
								DtoMapper.RecordParam,
								Expression.Add(DtoMapper.IndexParam, Expression.Constant(index)),
								Expression.Constant(1),
								DtoMapper.StateParam)));
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
				Expression.Lambda(initExpression, DtoMapper.RecordParam, DtoMapper.IndexParam, DtoMapper.StateParam).Compile();
		}

		throw new InvalidOperationException($"DTO {typeof(T).FullName} could not be created from fields: {string.Join(", ", fieldNameSet.Names)}");
	}

	private static string NormalizeFieldName(string text) => text.ReplaceOrdinal("_", "");

	private readonly DbDataMapper m_dataMapper;
	private readonly Dictionary<string, (DbDtoProperty<T> Property, DbTypeMapper Mapper)>? m_propertiesByNormalizedFieldName;
	private readonly ConcurrentDictionary<DtoMapper.FieldNameSet, Func<IDataRecord, int, DbConnectorRecordState?, T>> m_funcsByFieldNameSet = new();
}
