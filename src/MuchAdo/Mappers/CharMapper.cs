using System.Data;

namespace MuchAdo.Mappers;

internal sealed class CharMapper : NonNullableValueMapper<char>
{
	public override char MapNotNullField(IDataRecord record, int index) => record.GetChar(index);
}
