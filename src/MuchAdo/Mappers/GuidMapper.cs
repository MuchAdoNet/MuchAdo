using System.Data;

namespace MuchAdo.Mappers;

internal sealed class GuidMapper : NonNullableValueMapper<Guid>
{
	public override Guid MapNotNullField(IDataRecord record, int index) => record.GetGuid(index);
}
