using System.Data;

namespace MuchAdo.Mappers;

internal sealed class Int16Mapper(DbDataMapper dataMapper) : SingleFieldValueMapper<short>(dataMapper)
{
	public override short MapNotNullField(IDataRecord record, int index, DbConnectorRecordState? state) => record.GetInt16(index);
}
