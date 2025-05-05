using System.Data;

namespace MuchAdo.Mappers;

internal sealed class CharMapper(DbDataMapper dataMapper) : SingleFieldValueMapper<char>(dataMapper)
{
	public override char MapNotNullField(IDataRecord record, int index, DbConnectorRecordState? state) => record.GetChar(index);
}
