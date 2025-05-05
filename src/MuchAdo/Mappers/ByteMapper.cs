using System.Data;

namespace MuchAdo.Mappers;

internal sealed class ByteMapper(DbDataMapper dataMapper) : SingleFieldValueMapper<byte>(dataMapper)
{
	public override byte MapNotNullField(IDataRecord record, int index, DbConnectorRecordState? state) => record.GetByte(index);
}
