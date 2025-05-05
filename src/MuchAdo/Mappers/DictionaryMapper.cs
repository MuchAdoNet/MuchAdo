using System.Data;

namespace MuchAdo.Mappers;

internal sealed class DictionaryMapper<TDictionary, TValue>(DbDataMapper dataMapper) : DbTypeMapper<TDictionary>
{
	public override int? FieldCount => null;

	protected override TDictionary MapCore(IDataRecord record, int index, int count, DbConnectorRecordState? state)
	{
		var dictionary = new Dictionary<string, TValue>();
		var typeMapper = dataMapper.GetTypeMapper<TValue>();
		var notNull = false;
		for (var i = index; i < index + count; i++)
		{
			var name = record.GetName(i);
			dictionary[name] = typeMapper.Map(record, i, state);
			if (!notNull && !record.IsDBNull(i))
				notNull = true;
		}
		return notNull ? (TDictionary) (object) dictionary : default!;
	}
}
