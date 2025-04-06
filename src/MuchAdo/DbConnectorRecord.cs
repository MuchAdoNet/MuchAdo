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
	public T Get<T>() => m_mapper.GetTypeMapper<T>().Map(m_record, m_state);

	/// <summary>
	/// Converts the specified record field to the specified type.
	/// </summary>
	public T Get<T>(int index) => m_mapper.GetTypeMapper<T>().Map(m_record, index, m_state);

	/// <summary>
	/// Converts the specified record fields to the specified type.
	/// </summary>
	public T Get<T>(int index, int count) => m_mapper.GetTypeMapper<T>().Map(m_record, index, count, m_state);

	/// <summary>
	/// Converts the specified record field to the specified type.
	/// </summary>
	public T Get<T>(string name) => m_mapper.GetTypeMapper<T>().Map(m_record, m_record.GetOrdinal(name), 1, m_state);

	/// <summary>
	/// Converts the specified record fields to the specified type.
	/// </summary>
	public T Get<T>(string name, int count) => m_mapper.GetTypeMapper<T>().Map(m_record, m_record.GetOrdinal(name), count, m_state);

	/// <summary>
	/// Converts the specified record fields to the specified type.
	/// </summary>
	public T Get<T>(string fromName, string toName)
	{
		var fromIndex = m_record.GetOrdinal(fromName);
		var toIndex = m_record.GetOrdinal(toName);
		return m_mapper.GetTypeMapper<T>().Map(m_record, fromIndex, toIndex - fromIndex + 1, m_state);
	}

#if !NETSTANDARD2_0
	/// <summary>
	/// Converts the specified record field to the specified type.
	/// </summary>
	public T Get<T>(Index index) => m_mapper.GetTypeMapper<T>().Map(m_record, index.GetOffset(m_record.FieldCount), 1, m_state);

	/// <summary>
	/// Converts the specified record fields to the specified type.
	/// </summary>
	public T Get<T>(Range range)
	{
		var (index, count) = range.GetOffsetAndLength(m_record.FieldCount);
		return m_mapper.GetTypeMapper<T>().Map(m_record, index, count, m_state);
	}
#endif

	internal DbConnectorRecord(IDataRecord record, DbDataMapper mapper, DbConnectorRecordState? state)
	{
		m_record = record;
		m_mapper = mapper;
		m_state = state;
	}

	private readonly IDataRecord m_record;
	private readonly DbDataMapper m_mapper;
	private readonly DbConnectorRecordState? m_state;
}
