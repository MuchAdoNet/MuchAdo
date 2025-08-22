using System.Data;

namespace MuchAdo.Mappers;

internal sealed class NullableValueMapper<T>(DbTypeMapper<T> mapper) : DbTypeMapper<T?>
	where T : struct
{
	public override int? FieldCount => mapper.FieldCount;

	protected override T? MapCore(IDataRecord record, int index, int count, DbConnectorRecordState? state) =>
		!record.IsDBNull(index) ? mapper.Map(record, index, count, state) : null;
}
