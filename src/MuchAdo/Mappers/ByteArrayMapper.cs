using System.Data;

namespace MuchAdo.Mappers;

internal sealed class ByteArrayMapper : ReferenceValueMapper<byte[]>
{
	public override byte[] MapNotNullField(IDataRecord record, int index)
	{
		if (record.GetValue(index) is byte[] blob)
			return blob;

		var byteCount = (int) record.GetBytes(index, fieldOffset: 0, buffer: null, bufferoffset: 0, length: 0);
		var bytes = new byte[byteCount];
		record.GetBytes(index, fieldOffset: 0, buffer: bytes, bufferoffset: 0, length: byteCount);
		return bytes;
	}
}
