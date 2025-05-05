using System.Data;

namespace MuchAdo.Mappers;

internal sealed class ByteArrayMapper(DbDataMapper dataMapper) : SingleFieldReferenceMapper<byte[]>(dataMapper)
{
	public override byte[] MapNotNullField(IDataRecord record, int index, DbConnectorRecordState? state)
	{
		if (record.GetValue(index) is byte[] blob)
			return blob;

		var byteCount = (int) record.GetBytes(index, fieldOffset: 0, buffer: null, bufferoffset: 0, length: 0);
		var bytes = new byte[byteCount];
		record.GetBytes(index, fieldOffset: 0, buffer: bytes, bufferoffset: 0, length: byteCount);
		return bytes;
	}
}
