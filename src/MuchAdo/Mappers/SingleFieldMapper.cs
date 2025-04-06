using System.Data;

namespace MuchAdo.Mappers;

internal abstract class SingleFieldMapper<T> : DbTypeMapper<T>
{
	public override int? FieldCount => 1;

	protected sealed override T MapCore(IDataRecord record, int index, int count, DbConnectorRecordState? state) =>
		count == 1 ? MapField(record, index) : throw BadFieldCount(count);

	protected abstract T MapField(IDataRecord record, int index);
}
