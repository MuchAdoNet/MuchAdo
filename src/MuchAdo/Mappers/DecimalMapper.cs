using System.Data;

namespace MuchAdo.Mappers;

internal sealed class DecimalMapper(DbDataMapper dataMapper) : SingleFieldValueMapper<decimal>(dataMapper)
{
	public override decimal MapNotNullField(IDataRecord record, int index, DbConnectorRecordState? state) => record.GetDecimal(index);
}
