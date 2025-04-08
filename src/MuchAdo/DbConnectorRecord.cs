namespace MuchAdo;

/// <summary>
/// Converts the fields of a data record.
/// </summary>
public sealed class DbConnectorRecord
{
	/// <summary>
	/// Converts the record to the specified type.
	/// </summary>
	public T Get<T>() => m_connector.DataMapper.GetTypeMapper<T>().Map(m_connector.ActiveReader, m_state);

	/// <summary>
	/// Converts the specified record field to the specified type.
	/// </summary>
	public T Get<T>(int index) => m_connector.DataMapper.GetTypeMapper<T>().Map(m_connector.ActiveReader, index, m_state);

	/// <summary>
	/// Converts the specified record fields to the specified type.
	/// </summary>
	public T Get<T>(int index, int count) => m_connector.DataMapper.GetTypeMapper<T>().Map(m_connector.ActiveReader, index, count, m_state);

	/// <summary>
	/// Converts the specified record field to the specified type.
	/// </summary>
	public T Get<T>(string name)
	{
		var record = m_connector.ActiveReader;
		return m_connector.DataMapper.GetTypeMapper<T>().Map(record, record.GetOrdinal(name), 1, m_state);
	}

	/// <summary>
	/// Converts the specified record fields to the specified type.
	/// </summary>
	public T Get<T>(string name, int count)
	{
		var record = m_connector.ActiveReader;
		return m_connector.DataMapper.GetTypeMapper<T>().Map(record, record.GetOrdinal(name), count, m_state);
	}

	/// <summary>
	/// Converts the specified record fields to the specified type.
	/// </summary>
	public T Get<T>(string fromName, string toName)
	{
		var record = m_connector.ActiveReader;
		var fromIndex = record.GetOrdinal(fromName);
		var toIndex = record.GetOrdinal(toName);
		return m_connector.DataMapper.GetTypeMapper<T>().Map(record, fromIndex, toIndex - fromIndex + 1, m_state);
	}

#if !NETSTANDARD2_0
	/// <summary>
	/// Converts the specified record field to the specified type.
	/// </summary>
	public T Get<T>(Index index)
	{
		var record = m_connector.ActiveReader;
		return m_connector.DataMapper.GetTypeMapper<T>().Map(record, index.GetOffset(record.FieldCount), 1, m_state);
	}

	/// <summary>
	/// Converts the specified record fields to the specified type.
	/// </summary>
	public T Get<T>(Range range)
	{
		var record = m_connector.ActiveReader;
		var (index, count) = range.GetOffsetAndLength(record.FieldCount);
		return m_connector.DataMapper.GetTypeMapper<T>().Map(record, index, count, m_state);
	}
#endif

	internal DbConnectorRecord(DbConnector connector, DbConnectorRecordState? state)
	{
		m_connector = connector;
		m_state = state;
	}

	private readonly DbConnector m_connector;
	private readonly DbConnectorRecordState? m_state;
}
