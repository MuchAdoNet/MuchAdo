using System.Data;

namespace MuchAdo.Mappers;

internal sealed class DateTimeMapper(DbDataMapper dataMapper) : SingleFieldValueMapper<DateTime>(dataMapper)
{
	public override DateTime MapNotNullField(IDataRecord record, int index, DbConnectorRecordState? state) => record.GetDateTime(index);
}
