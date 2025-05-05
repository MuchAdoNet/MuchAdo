using System.Data;
using MuchAdo.Mappers;
using MySqlConnector;

namespace MuchAdo.MySql;

internal sealed class MySqlDbTypeMapperFactory : DbTypeMapperFactory
{
	public override DbTypeMapper<T>? TryCreateTypeMapper<T>(DbDataMapper dataMapper)
	{
		if (typeof(T) == typeof(MySqlDateTime))
			return (DbTypeMapper<T>) (object) new MySqlDateTimeMapper(dataMapper);
		if (typeof(T) == typeof(MySqlDecimal))
			return (DbTypeMapper<T>) (object) new MySqlDecimalMapper(dataMapper);
		if (typeof(T) == typeof(MySqlGeometry))
			return (DbTypeMapper<T>) (object) new MySqlGeometryMapper(dataMapper);

		return null;
	}

	private sealed class MySqlDateTimeMapper(DbDataMapper dataMapper) : SingleFieldValueMapper<MySqlDateTime>(dataMapper)
	{
		public override MySqlDateTime MapNotNullField(IDataRecord record, int index, DbConnectorRecordState? state) => ((MySqlDataReader) record).GetMySqlDateTime(index);
	}

	private sealed class MySqlDecimalMapper(DbDataMapper dataMapper) : SingleFieldValueMapper<MySqlDecimal>(dataMapper)
	{
		public override MySqlDecimal MapNotNullField(IDataRecord record, int index, DbConnectorRecordState? state) => ((MySqlDataReader) record).GetMySqlDecimal(index);
	}

	private sealed class MySqlGeometryMapper(DbDataMapper dataMapper) : SingleFieldReferenceMapper<MySqlGeometry>(dataMapper)
	{
		public override MySqlGeometry MapNotNullField(IDataRecord record, int index, DbConnectorRecordState? state) => ((MySqlDataReader) record).GetMySqlGeometry(index);
	}
}
