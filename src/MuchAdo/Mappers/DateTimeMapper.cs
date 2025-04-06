using System.Data;

namespace MuchAdo.Mappers;

internal sealed class DateTimeMapper : NonNullableValueMapper<DateTime>
{
	public override DateTime MapNotNullField(IDataRecord record, int index) => record.GetDateTime(index);
}
