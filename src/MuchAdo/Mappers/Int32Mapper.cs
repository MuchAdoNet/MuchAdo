using System.Data;

namespace MuchAdo.Mappers;

internal sealed class Int32Mapper(DbDataMapper dataMapper) : SingleFieldValueMapper<int>(dataMapper)
{
	public override int MapNotNullField(IDataRecord record, int index, DbConnectorRecordState? state) => record.GetInt32(index);
}
