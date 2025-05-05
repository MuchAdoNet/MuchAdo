using System.Data;

namespace MuchAdo.Mappers;

internal sealed class DoubleMapper(DbDataMapper dataMapper) : SingleFieldValueMapper<double>(dataMapper)
{
	public override double MapNotNullField(IDataRecord record, int index, DbConnectorRecordState? state) => record.GetDouble(index);
}
