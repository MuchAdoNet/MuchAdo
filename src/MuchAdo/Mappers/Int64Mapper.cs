using System.Data;

namespace MuchAdo.Mappers;

internal sealed class Int64Mapper : NonNullableValueMapper<long>
{
	public override long MapNotNullField(IDataRecord record, int index) => record.GetInt64(index);
}
