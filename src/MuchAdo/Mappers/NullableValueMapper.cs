using System.Data;

namespace MuchAdo.Mappers;

internal sealed class NullableValueMapper<T>(DbDataMapper dataMapper, SingleFieldValueMapper<T> mapper) : SingleFieldMapper<T?>(dataMapper)
	where T : struct
{
	protected override T? MapField(IDataRecord record, int index, DbConnectorRecordState? state) =>
		!record.IsDBNull(index) ? mapper.MapNotNullField(record, index, state) : null;
}
