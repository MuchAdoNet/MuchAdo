using System.Data;

namespace MuchAdo.Mappers;

internal abstract class ValueTupleMapperBase<T>(DbTypeMapper[] mappers) : DbTypeMapper<T>
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
