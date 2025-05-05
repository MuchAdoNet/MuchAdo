using System.Data;

namespace MuchAdo.Mappers;

public abstract class SingleFieldReferenceMapper<T>(DbDataMapper dataMapper) : SingleFieldMapper<T?>(dataMapper)
	where T : class
{
	protected sealed override T? MapField(IDataRecord record, int index, DbConnectorRecordState? state) =>
		!record.IsDBNull(index) ? MapNotNullField(record, index, state) : null;

	public abstract T MapNotNullField(IDataRecord record, int index, DbConnectorRecordState? state);
}
