using System.Data;
using System.Data.Common;

namespace MuchAdo.Mappers;

internal sealed class TextReaderMapper(DbDataMapper dataMapper) : SingleFieldReferenceMapper<TextReader>(dataMapper)
{
	public override TextReader MapNotNullField(IDataRecord record, int index, DbConnectorRecordState? state)
	{
		if (record is DbDataReader dbReader)
			return dbReader.GetTextReader(index);

		return new StringReader(record.GetString(index));
	}
}
