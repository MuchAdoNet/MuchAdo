namespace MuchAdo;

/// <summary>
/// Saves type mapping state for a record.
/// </summary>
public sealed class DbRecordState
{
	/// <summary>
	/// Gets the state for the specified mapper, index, and count; returns null if none.
	/// </summary>
	public object? Get(DbTypeMapper mapper, int index, int count) =>
		m_states?.GetValueOrDefault((mapper, index, count));

	/// <summary>
	/// Sets the state for the specified mapper, index, and count.
	/// </summary>
	public void Set(DbTypeMapper mapper, int index, int count, object? state) =>
		(m_states ??= [])[(mapper, index, count)] = state;

	private Dictionary<(DbTypeMapper Mapper, int Index, int Count), object?>? m_states;
}
