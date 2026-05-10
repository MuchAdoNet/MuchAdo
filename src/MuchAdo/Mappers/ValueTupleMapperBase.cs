using System.Data;

namespace MuchAdo.Mappers;

internal abstract class ValueTupleMapperBase<T> : DbTypeMapper<T>
{
	public override int? FieldCount { get; }

	protected ValueTupleMapperBase(DbDataMapper dataMapper, DbTypeMapper[] typeMappers)
	{
		m_dataMapper = dataMapper;
		m_typeMappers = typeMappers;
		FieldCount = CalculateFieldCount();
	}

	protected void GetValueRanges(IDataRecord record, int index, int count, Span<(int Index, int Count)> valueRanges)
	{
		if (FieldCount is { } requiredFieldCount && !(count == requiredFieldCount || (count > requiredFieldCount && m_dataMapper.IgnoreUnusedFields)))
			throw BadFieldCount(count);

		var valueCount = m_typeMappers.Length;
		var recordIndex = index;
		for (var valueIndex = 0; valueIndex < valueCount; valueIndex++)
		{
			var mapperFieldCount = m_typeMappers[valueIndex].FieldCount;

			int fieldCount;
			int? nullIndex = null;
			if (mapperFieldCount is null)
			{
				int? remainingFieldCount = 0;
				var minimumRemainingFieldCount = 0;
				for (var nextValueIndex = valueIndex + 1; nextValueIndex < valueCount; nextValueIndex++)
				{
					var nextFieldCount = m_typeMappers[nextValueIndex].FieldCount;
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

	private int? CalculateFieldCount()
	{
		var totalFieldCount = 0;
		foreach (var typeMapper in m_typeMappers)
		{
			if (typeMapper.FieldCount is not { } fieldCount)
				return null;
			totalFieldCount += fieldCount;
		}
		return totalFieldCount;
	}

	private readonly DbDataMapper m_dataMapper;
	private readonly DbTypeMapper[] m_typeMappers;
}
