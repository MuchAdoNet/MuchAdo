using System.Data;
using System.Data.Common;

namespace MuchAdo.Mappers;

internal sealed class GetFieldValueMapper<T>(DbDataMapper dataMapper) : SingleFieldValueMapper<T>(dataMapper)
	where T : struct
{
	public override T MapNotNullField(IDataRecord record, int index, DbConnectorRecordState? state) =>
		(record as DbDataReader ?? throw new InvalidOperationException("Record must be a DbDataRecord.")).GetFieldValue<T>(index);
}
