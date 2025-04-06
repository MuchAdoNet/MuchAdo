using System.Data;

namespace MuchAdo.Mappers;

internal sealed class FloatMapper : NonNullableValueMapper<float>
{
	public override float MapNotNullField(IDataRecord record, int index) => record.GetFloat(index);
}
