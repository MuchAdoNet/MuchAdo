using System.Data;
using System.Runtime.CompilerServices;

namespace MuchAdo;

/// <summary>
/// Encapsulates multiple result sets.
/// </summary>
public sealed class DbConnectorResultSets : IDisposable, IAsyncDisposable
{
	/// <summary>
	/// Reads a result set, converting each record to the specified type.
	/// </summary>
	/// <seealso cref="ReadAsync{T}(CancellationToken)" />
	public IReadOnlyList<T> Read<T>() =>
		DoRead<T>(null);

	/// <summary>
	/// Reads a result set, converting each record to the specified type with the specified delegate.
	/// </summary>
	/// <seealso cref="ReadAsync{T}(Func{DbRecord, T}, CancellationToken)" />
	public IReadOnlyList<T> Read<T>(Func<DbRecord, T> map) =>
		DoRead(map ?? throw new ArgumentNullException(nameof(map)));

	/// <summary>
	/// Reads a result set, converting each record to the specified type.
	/// </summary>
	/// <seealso cref="Read{T}()" />
	public ValueTask<IReadOnlyList<T>> ReadAsync<T>(CancellationToken cancellationToken = default) =>
		DoReadAsync<T>(null, cancellationToken);

	/// <summary>
	/// Reads a result set, converting each record to the specified type with the specified delegate.
	/// </summary>
	/// <seealso cref="Read{T}(Func{DbRecord, T})" />
	public ValueTask<IReadOnlyList<T>> ReadAsync<T>(Func<DbRecord, T> map, CancellationToken cancellationToken = default) =>
		DoReadAsync(map ?? throw new ArgumentNullException(nameof(map)), cancellationToken);

	/// <summary>
	/// Reads a result set, reading one record at a time and converting it to the specified type.
	/// </summary>
	/// <seealso cref="EnumerateAsync{T}(CancellationToken)" />
	public IEnumerable<T> Enumerate<T>() =>
		DoEnumerate<T>(null);

	/// <summary>
	/// Reads a result set, reading one record at a time and converting it to the specified type with the specified delegate.
	/// </summary>
	/// <seealso cref="EnumerateAsync{T}(Func{DbRecord, T}, CancellationToken)" />
	public IEnumerable<T> Enumerate<T>(Func<DbRecord, T> map) =>
		DoEnumerate(map ?? throw new ArgumentNullException(nameof(map)));

	/// <summary>
	/// Reads a result set, reading one record at a time and converting it to the specified type.
	/// </summary>
	/// <seealso cref="Enumerate{T}()" />
	public IAsyncEnumerable<T> EnumerateAsync<T>(CancellationToken cancellationToken = default) =>
		DoEnumerateAsync<T>(null, cancellationToken);

	/// <summary>
	/// Reads a result set, reading one record at a time and converting it to the specified type with the specified delegate.
	/// </summary>
	/// <seealso cref="Enumerate{T}(Func{DbRecord, T})" />
	public IAsyncEnumerable<T> EnumerateAsync<T>(Func<DbRecord, T> map, CancellationToken cancellationToken = default) =>
		DoEnumerateAsync(map ?? throw new ArgumentNullException(nameof(map)), cancellationToken);

	/// <summary>
	/// Disposes resources used by the result sets.
	/// </summary>
	/// <seealso cref="DisposeAsync" />
	public void Dispose()
	{
		m_reader.Dispose();
		m_command.Dispose();
	}

	/// <summary>
	/// Disposes resources used by the result sets.
	/// </summary>
	/// <seealso cref="Dispose" />
	public async ValueTask DisposeAsync()
	{
		await m_methods.DisposeReaderAsync(m_reader).ConfigureAwait(false);
		await m_methods.DisposeCommandAsync(m_command).ConfigureAwait(false);
	}

	internal DbConnectorResultSets(IDbCommand command, IDataReader reader, DbProviderMethods methods, DbDataMapper mapper)
	{
		m_command = command;
		m_reader = reader;
		m_methods = methods;
		m_mapper = mapper;
	}

	private IReadOnlyList<T> DoRead<T>(Func<DbRecord, T>? map)
	{
		if (m_next && !m_reader.NextResult())
			throw CreateNoMoreResultsException();
		m_next = true;

		var list = new List<T>();
		var record = new DbRecord(m_reader, m_mapper, new DbRecordState());
		while (m_reader.Read())
			list.Add(map is not null ? map(record) : record.Get<T>());
		return list;
	}

	private async ValueTask<IReadOnlyList<T>> DoReadAsync<T>(Func<DbRecord, T>? map, CancellationToken cancellationToken)
	{
		if (m_next && !await m_methods.NextResultAsync(m_reader, cancellationToken).ConfigureAwait(false))
			throw CreateNoMoreResultsException();
		m_next = true;

		var list = new List<T>();
		var record = new DbRecord(m_reader, m_mapper, new DbRecordState());
		while (await m_methods.ReadAsync(m_reader, cancellationToken).ConfigureAwait(false))
			list.Add(map is not null ? map(record) : record.Get<T>());
		return list;
	}

	private IEnumerable<T> DoEnumerate<T>(Func<DbRecord, T>? map)
	{
		if (m_next && !m_reader.NextResult())
			throw CreateNoMoreResultsException();
		m_next = true;

		var record = new DbRecord(m_reader, m_mapper, new DbRecordState());
		while (m_reader.Read())
			yield return map is not null ? map(record) : record.Get<T>();
	}

	private async IAsyncEnumerable<T> DoEnumerateAsync<T>(Func<DbRecord, T>? map, [EnumeratorCancellation] CancellationToken cancellationToken)
	{
		if (m_next && !await m_methods.NextResultAsync(m_reader, cancellationToken).ConfigureAwait(false))
			throw CreateNoMoreResultsException();
		m_next = true;

		var record = new DbRecord(m_reader, m_mapper, new DbRecordState());
		while (await m_methods.ReadAsync(m_reader, cancellationToken).ConfigureAwait(false))
			yield return map is not null ? map(record) : record.Get<T>();
	}

	private static InvalidOperationException CreateNoMoreResultsException() => new("No more results.");

	private readonly IDbCommand m_command;
	private readonly IDataReader m_reader;
	private readonly DbProviderMethods m_methods;
	private readonly DbDataMapper m_mapper;
	private bool m_next;
}
