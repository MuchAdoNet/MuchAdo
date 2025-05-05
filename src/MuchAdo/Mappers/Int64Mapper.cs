using System.Data;

namespace MuchAdo.Mappers;

internal sealed class Int64Mapper(DbDataMapper dataMapper) : SingleFieldValueMapper<long>(dataMapper)
{
	public override long MapNotNullField(IDataRecord record, int index, DbConnectorRecordState? state) => record.GetInt64(index);
}
