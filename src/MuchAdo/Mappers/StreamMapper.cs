using System.Data;
using System.Data.Common;

namespace MuchAdo.Mappers;

internal sealed class StreamMapper : ReferenceValueMapper<Stream>
{
	public override Stream MapNotNullField(IDataRecord record, int index)
	{
		if (record is DbDataReader dbReader)
			return dbReader.GetStream(index);

		var byteCount = (int) record.GetBytes(index, fieldOffset: 0, buffer: null, bufferoffset: 0, length: 0);
		var bytes = new byte[byteCount];
		record.GetBytes(index, fieldOffset: 0, buffer: bytes, bufferoffset: 0, length: byteCount);
		return new MemoryStream(buffer: bytes, index: 0, count: byteCount, writable: false);
	}
}
