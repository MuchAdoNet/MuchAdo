using System.Data;

namespace MuchAdo.Mappers;

internal sealed class DoubleMapper : NonNullableValueMapper<double>
{
	public override double MapNotNullField(IDataRecord record, int index) => record.GetDouble(index);
}
