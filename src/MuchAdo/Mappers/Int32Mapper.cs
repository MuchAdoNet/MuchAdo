using System.Data;

namespace MuchAdo.Mappers;

internal sealed class Int32Mapper : NonNullableValueMapper<int>
{
	public override int MapNotNullField(IDataRecord record, int index) => record.GetInt32(index);
}
