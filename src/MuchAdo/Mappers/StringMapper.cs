using System.Data;

namespace MuchAdo.Mappers;

internal sealed class StringMapper(DbDataMapper dataMapper) : SingleFieldReferenceMapper<string>(dataMapper)
{
	public override string MapNotNullField(IDataRecord record, int index, DbConnectorRecordState? state) => record.GetString(index);
}
