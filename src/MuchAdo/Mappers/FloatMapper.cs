using System.Data;

namespace MuchAdo.Mappers;

internal sealed class FloatMapper(DbDataMapper dataMapper) : SingleFieldValueMapper<float>(dataMapper)
{
	public override float MapNotNullField(IDataRecord record, int index, DbConnectorRecordState? state) => record.GetFloat(index);
}
