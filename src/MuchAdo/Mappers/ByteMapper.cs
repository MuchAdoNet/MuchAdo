using System.Data;

namespace MuchAdo.Mappers;

internal sealed class ByteMapper : NonNullableValueMapper<byte>
{
	public override byte MapNotNullField(IDataRecord record, int index) => record.GetByte(index);
}
