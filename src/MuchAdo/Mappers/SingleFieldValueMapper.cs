using System.Data;

namespace MuchAdo.Mappers;

public abstract class SingleFieldValueMapper<T>(DbDataMapper dataMapper) : SingleFieldMapper<T>(dataMapper)
	where T : struct
{
	protected sealed override T MapField(IDataRecord record, int index, DbConnectorRecordState? state) =>
		!record.IsDBNull(index) ? MapNotNullField(record, index, state) : throw NotNullable();

	public abstract T MapNotNullField(IDataRecord record, int index, DbConnectorRecordState? state);
}
