using System.Data;

namespace MuchAdo.Mappers;

internal abstract class NonNullableValueMapper<T> : SingleFieldMapper<T>
	where T : struct
{
	protected sealed override T MapField(IDataRecord record, int index) =>
		!record.IsDBNull(index) ? MapNotNullField(record, index) : throw NotNullable();

	public abstract T MapNotNullField(IDataRecord record, int index);
}
