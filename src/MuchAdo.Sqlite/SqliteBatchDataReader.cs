using System.Data;
using System.Diagnostics;
using Microsoft.Data.Sqlite;

namespace MuchAdo.Sqlite;

/// <summary>
/// <para>
/// Wraps multiple <see cref="SqliteCommand"/> executions and exposes them as a single <see cref="IDataReader"/>
/// with usable <see cref="IDataReader.NextResult"/> semantics.
/// </para>
/// <para>
/// Why this exists: MuchAdo represents a chained <c>CommandFormat</c> sequence as multiple commands. Providers that
/// support <c>DbBatch</c> can execute those as a true batch; however, Microsoft.Data.Sqlite does not support
/// <c>DbConnection.CreateBatch()</c>. While Microsoft.Data.Sqlite supports <em>multi-statement</em> command text
/// batching (where <see cref="SqliteDataReader.NextResult"/> advances to the next statement), that behavior does
/// not apply when the batch is modeled as multiple separate <see cref="SqliteCommand"/> instances.
/// </para>
/// <para>
/// This type bridges that gap by switching the inner <see cref="SqliteDataReader"/> to the next command when the
/// current reader has no more result sets.
/// </para>
/// </summary>
internal sealed class SqliteBatchDataReader : IDataReader
{
	public SqliteBatchDataReader(SqliteBatch batch, CommandBehavior behavior)
	{
		m_batch = batch;
		m_behavior = behavior;
		m_timeoutRemaining = batch.Timeout;

		if (m_timeoutRemaining is not null)
			m_stopwatch = Stopwatch.StartNew();

		if (batch.Commands.Count == 0)
			throw new InvalidOperationException("The batch is empty.");

		NextResult();
	}

	public void Dispose()
	{
		if (m_isDisposed)
			return;

		m_isDisposed = true;
		m_inner?.Dispose();
		m_inner = null;
	}

	public string GetName(int i) => Inner.GetName(i);
	public string GetDataTypeName(int i) => Inner.GetDataTypeName(i);
	public Type GetFieldType(int i) => Inner.GetFieldType(i);
	public object GetValue(int i) => Inner.GetValue(i);
	public int GetValues(object[] values) => Inner.GetValues(values);
	public int GetOrdinal(string name) => Inner.GetOrdinal(name);
	public bool GetBoolean(int i) => Inner.GetBoolean(i);
	public byte GetByte(int i) => Inner.GetByte(i);
	public long GetBytes(int i, long fieldOffset, byte[]? buffer, int bufferoffset, int length) => Inner.GetBytes(i, fieldOffset, buffer, bufferoffset, length);
	public char GetChar(int i) => Inner.GetChar(i);
	public long GetChars(int i, long fieldoffset, char[]? buffer, int bufferoffset, int length) => Inner.GetChars(i, fieldoffset, buffer, bufferoffset, length);
	public Guid GetGuid(int i) => Inner.GetGuid(i);
	public short GetInt16(int i) => Inner.GetInt16(i);
	public int GetInt32(int i) => Inner.GetInt32(i);
	public long GetInt64(int i) => Inner.GetInt64(i);
	public float GetFloat(int i) => Inner.GetFloat(i);
	public double GetDouble(int i) => Inner.GetDouble(i);
	public string GetString(int i) => Inner.GetString(i);
	public decimal GetDecimal(int i) => Inner.GetDecimal(i);
	public DateTime GetDateTime(int i) => Inner.GetDateTime(i);
	public IDataReader GetData(int i) => Inner.GetData(i);
	public bool IsDBNull(int i) => Inner.IsDBNull(i);

	public object this[int i] => Inner[i];
	public object this[string name] => Inner[name];

	public int Depth => Inner.Depth;
	public bool IsClosed => m_isDisposed || Inner.IsClosed;
	public int RecordsAffected => Inner.RecordsAffected;
	public int FieldCount => Inner.FieldCount;

	public void Close() => Dispose();

	public DataTable GetSchemaTable() => Inner.GetSchemaTable();

	public bool NextResult()
	{
		if (m_isDisposed)
			throw new ObjectDisposedException(nameof(SqliteBatchDataReader));

		while (m_commandIndex < m_batch.Commands.Count)
		{
			while (true)
			{
				if (m_inner is null)
				{
					var command = m_batch.Commands[m_commandIndex];
					if (m_timeoutRemaining is not null)
						command.CommandTimeout = m_timeoutRemaining.Value;
					m_inner = command.ExecuteReader(m_behavior);
					if (m_timeoutRemaining > 0)
						m_timeoutRemaining = Math.Max(1, m_timeoutRemaining.Value - (int) (m_stopwatch!.ElapsedMilliseconds / 1000));
				}
				else if (!m_inner.NextResult())
				{
					break;
				}

				// only read from commands with fields
				if (m_inner.FieldCount > 0)
					return true;
			}

			m_inner.Dispose();
			m_inner = null;
			m_commandIndex++;
		}

		return false;
	}

	public bool Read()
	{
		if (m_isDisposed)
			throw new ObjectDisposedException(nameof(SqliteBatchDataReader));

		return Inner.Read();
	}

	private SqliteDataReader Inner => m_inner ?? throw new ObjectDisposedException(nameof(SqliteBatchDataReader));

	private readonly SqliteBatch m_batch;
	private readonly CommandBehavior m_behavior;
	private readonly Stopwatch? m_stopwatch;
	private int? m_timeoutRemaining;
	private int m_commandIndex;
	private SqliteDataReader? m_inner;
	private bool m_isDisposed;
}
