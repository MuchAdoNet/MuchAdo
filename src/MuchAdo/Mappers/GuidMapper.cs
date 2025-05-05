using System.Data;

namespace MuchAdo.Mappers;

internal sealed class GuidMapper(DbDataMapper dataMapper) : SingleFieldValueMapper<Guid>(dataMapper)
{
	public override Guid MapNotNullField(IDataRecord record, int index, DbConnectorRecordState? state) => record.GetGuid(index);
}
