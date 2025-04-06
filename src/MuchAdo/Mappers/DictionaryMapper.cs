using System.Data;

namespace MuchAdo.Mappers;

internal sealed class DictionaryMapper<T> : DbTypeMapper<T>
{
	public override int? FieldCount => null;

	protected override T MapCore(IDataRecord record, int index, int count, DbConnectorRecordState? state)
	{
		var dictionary = new Dictionary<string, object?>();
		var notNull = false;
		for (var i = index; i < index + count; i++)
		{
			var name = record.GetName(i);
			if (!record.IsDBNull(i))
			{
				dictionary[name] = record.GetValue(i);
				notNull = true;
			}
			else
			{
				dictionary[name] = null;
			}
		}
		return notNull ? (T) (object) dictionary : default!;
	}
}
