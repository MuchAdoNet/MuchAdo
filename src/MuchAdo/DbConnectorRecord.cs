using System.Data;

namespace MuchAdo;

/// <summary>
/// Converts the fields of a data record.
/// </summary>
public sealed class DbConnectorRecord
{
	/// <summary>
	/// Converts the record to the specified type.
	/// </summary>
	public T Get<T>() => DataMapper.GetTypeMapper<T>().Map(ActiveReader!, m_state);

	/// <summary>
	/// Converts the specified record field to the specified type.
	/// </summary>
	public T Get<T>(int index) => DataMapper.GetTypeMapper<T>().Map(ActiveReader, index, m_state);

	/// <summary>
	/// Converts the specified record fields to the specified type.
	/// </summary>
	public T Get<T>(int index, int count) => DataMapper.GetTypeMapper<T>().Map(ActiveReader, index, count, m_state);

	/// <summary>
	/// Converts the specified record field to the specified type.
	/// </summary>
	public T Get<T>(string name)
	{
		var record = ActiveReader;
		return DataMapper.GetTypeMapper<T>().Map(record, record.GetOrdinal(name), 1, m_state);
	}

	/// <summary>
	/// Converts the specified record fields to the specified type.
	/// </summary>
	public T Get<T>(string name, int count)
	{
		var record = ActiveReader;
		return DataMapper.GetTypeMapper<T>().Map(record, record.GetOrdinal(name), count, m_state);
	}

#if NET
	/// <summary>
	/// Converts the specified record field to the specified type.
	/// </summary>
	public T Get<T>(Index index)
	{
		var record = ActiveReader;
		return DataMapper.GetTypeMapper<T>().Map(record, index.GetOffset(record.FieldCount), 1, m_state);
	}

	/// <summary>
	/// Converts the specified record fields to the specified type.
	/// </summary>
	public T Get<T>(Range range)
	{
		var record = ActiveReader;
		var (index, count) = range.GetOffsetAndLength(record.FieldCount);
		return DataMapper.GetTypeMapper<T>().Map(record, index, count, m_state);
	}
#endif

	/// <summary>
	/// Gets the number of fields in the record.
	/// </summary>
	public int FieldCount => ActiveReader.FieldCount;

	/// <summary>
	/// Returns the index of the field with the specified name.
	/// </summary>
	public int GetOrdinal(string name) => ActiveReader.GetOrdinal(name);

	/// <summary>
	/// Returns the name of the field at the specified index.
	/// </summary>
	public string GetName(int index) => ActiveReader.GetName(index);

	/// <summary>
	/// Returns the underlying data record, casting it to the specified type.
	/// </summary>
	public T As<T>()
		where T : IDataRecord =>
		(T) ActiveReader;

	internal DbConnectorRecord(DbConnector connector, DbConnectorRecordState? state)
	{
		m_connector = connector;
		m_state = state;
	}

	private DbDataMapper DataMapper => m_connector.DataMapper;

	private IDataReader ActiveReader => m_connector.ActiveReader ??
		throw new InvalidOperationException("Connector does not have an active reader.");

	private readonly DbConnector m_connector;
	private readonly DbConnectorRecordState? m_state;
}
