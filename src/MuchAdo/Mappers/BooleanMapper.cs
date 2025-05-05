using System.Data;

namespace MuchAdo.Mappers;

internal sealed class BooleanMapper(DbDataMapper dataMapper) : SingleFieldValueMapper<bool>(dataMapper)
{
	public override bool MapNotNullField(IDataRecord record, int index, DbConnectorRecordState? state) => record.GetBoolean(index);
}
