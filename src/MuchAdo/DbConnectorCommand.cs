using System.Collections;
using System.Data;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace MuchAdo;

/// <summary>
/// Encapsulates the text and parameters of a database command.
/// </summary>
public sealed class DbConnectorCommand
{
	/// <summary>
	/// The text of the command.
	/// </summary>
	public string Text { get; }

	/// <summary>
	/// The parameters of the command.
	/// </summary>
	public DbParameters Parameters => m_parameters;

	/// <summary>
	/// The <see cref="CommandType"/> of the command.
	/// </summary>
	public CommandType CommandType { get; }

	/// <summary>
	/// The timeout of the command.
	/// </summary>
	/// <remarks>If not specified, the default timeout for the connection is used.</remarks>
	public TimeSpan? Timeout { get; private set; }

	/// <summary>
	/// True after <see cref="Cache"/> is called.
	/// </summary>
	public bool IsCached { get; private set; }

	/// <summary>
	/// True after <see cref="Prepare"/> is called.
	/// </summary>
	public bool IsPrepared { get; private set; }

	/// <summary>
	/// The connector of the command.
	/// </summary>
	public DbConnector Connector { get; }

	/// <summary>
	/// Executes the command, returning the number of rows affected.
	/// </summary>
	/// <seealso cref="ExecuteAsync" />
	public int Execute()
	{
		using var commandScope = CreateCommand();
		return Connector.ExecuteNonQueryCore();
	}

	/// <summary>
	/// Executes the command, returning the number of rows affected.
	/// </summary>
	/// <seealso cref="Execute" />
	public async ValueTask<int> ExecuteAsync(CancellationToken cancellationToken = default)
	{
		await using var commandScope = (await CreateCommandAsync(cancellationToken).ConfigureAwait(false)).ConfigureAwait(false);
		return await Connector.ExecuteNonQueryCoreAsync(cancellationToken).ConfigureAwait(false);
	}

	/// <summary>
	/// Executes the query, reading every record and converting it to the specified type.
	/// </summary>
	/// <seealso cref="QueryAsync{T}(CancellationToken)" />
	public IReadOnlyList<T> Query<T>() =>
		DoQuery<T>(null);

	/// <summary>
	/// Executes the query, reading every record and converting it to the specified type with the specified delegate.
	/// </summary>
	/// <seealso cref="QueryAsync{T}(Func{DbConnectorRecord, T}, CancellationToken)" />
	public IReadOnlyList<T> Query<T>(Func<DbConnectorRecord, T> map) =>
		DoQuery(map ?? throw new ArgumentNullException(nameof(map)));

	/// <summary>
	/// Executes the query, converting the first record to the specified type.
	/// </summary>
	/// <remarks>Throws <see cref="InvalidOperationException"/> if no records are returned.</remarks>
	/// <seealso cref="QueryFirstAsync{T}(CancellationToken)" />
	public T QueryFirst<T>() =>
		DoQueryFirst<T>(null, single: false, orDefault: false);

	/// <summary>
	/// Executes the query, converting the first record to the specified type with the specified delegate.
	/// </summary>
	/// <remarks>Throws <see cref="InvalidOperationException"/> if no records are returned.</remarks>
	/// <seealso cref="QueryFirstAsync{T}(Func{DbConnectorRecord, T}, CancellationToken)" />
	public T QueryFirst<T>(Func<DbConnectorRecord, T> map) =>
		DoQueryFirst(map ?? throw new ArgumentNullException(nameof(map)), single: false, orDefault: false);

	/// <summary>
	/// Executes the query, converting the first record to the specified type.
	/// </summary>
	/// <remarks>Returns <c>default(T)</c> if no records are returned.</remarks>
	/// <seealso cref="QueryFirstOrDefaultAsync{T}(CancellationToken)" />
	public T QueryFirstOrDefault<T>() =>
		DoQueryFirst<T>(null, single: false, orDefault: true);

	/// <summary>
	/// Executes the query, converting the first record to the specified type with the specified delegate.
	/// </summary>
	/// <remarks>Returns <c>default(T)</c> if no records are returned.</remarks>
	/// <seealso cref="QueryFirstOrDefaultAsync{T}(Func{DbConnectorRecord, T}, CancellationToken)" />
	public T QueryFirstOrDefault<T>(Func<DbConnectorRecord, T> map) =>
		DoQueryFirst(map ?? throw new ArgumentNullException(nameof(map)), single: false, orDefault: true);

	/// <summary>
	/// Executes the query, converting the first record to the specified type.
	/// </summary>
	/// <remarks>Throws <see cref="InvalidOperationException"/> if no records are returned, or if more than one record is returned.</remarks>
	/// <seealso cref="QuerySingleAsync{T}(CancellationToken)" />
	public T QuerySingle<T>() =>
		DoQueryFirst<T>(null, single: true, orDefault: false);

	/// <summary>
	/// Executes the query, converting the first record to the specified type with the specified delegate.
	/// </summary>
	/// <remarks>Throws <see cref="InvalidOperationException"/> if no records are returned, or if more than one record is returned.</remarks>
	/// <seealso cref="QuerySingleAsync{T}(Func{DbConnectorRecord, T}, CancellationToken)" />
	public T QuerySingle<T>(Func<DbConnectorRecord, T> map) =>
		DoQueryFirst(map ?? throw new ArgumentNullException(nameof(map)), single: true, orDefault: false);

	/// <summary>
	/// Executes the query, converting the first record to the specified type.
	/// </summary>
	/// <remarks>Returns <c>default(T)</c> if no records are returned.
	/// Throws <see cref="InvalidOperationException"/> if more than one record is returned.</remarks>
	/// <seealso cref="QuerySingleOrDefaultAsync{T}(CancellationToken)" />
	public T QuerySingleOrDefault<T>() =>
		DoQueryFirst<T>(null, single: true, orDefault: true);

	/// <summary>
	/// Executes the query, converting the first record to the specified type with the specified delegate.
	/// </summary>
	/// <remarks>Returns <c>default(T)</c> if no records are returned.
	/// Throws <see cref="InvalidOperationException"/> if more than one record is returned.</remarks>
	/// <seealso cref="QuerySingleOrDefaultAsync{T}(Func{DbConnectorRecord, T}, CancellationToken)" />
	public T QuerySingleOrDefault<T>(Func<DbConnectorRecord, T> map) =>
		DoQueryFirst(map ?? throw new ArgumentNullException(nameof(map)), single: true, orDefault: true);

	/// <summary>
	/// Executes the query, converting each record to the specified type.
	/// </summary>
	/// <seealso cref="Query{T}()" />
	public ValueTask<IReadOnlyList<T>> QueryAsync<T>(CancellationToken cancellationToken = default) =>
		DoQueryAsync<T>(null, cancellationToken);

	/// <summary>
	/// Executes the query, converting each record to the specified type with the specified delegate.
	/// </summary>
	/// <seealso cref="Query{T}(Func{DbConnectorRecord, T})" />
	public ValueTask<IReadOnlyList<T>> QueryAsync<T>(Func<DbConnectorRecord, T> map, CancellationToken cancellationToken = default) =>
		DoQueryAsync(map ?? throw new ArgumentNullException(nameof(map)), cancellationToken);

	/// <summary>
	/// Executes the query, converting the first record to the specified type.
	/// </summary>
	/// <remarks>Throws <see cref="InvalidOperationException"/> if no records are returned.</remarks>
	/// <seealso cref="QueryFirst{T}()" />
	public ValueTask<T> QueryFirstAsync<T>(CancellationToken cancellationToken = default) =>
		DoQueryFirstAsync<T>(null, single: false, orDefault: false, cancellationToken);

	/// <summary>
	/// Executes the query, converting the first record to the specified type with the specified delegate.
	/// </summary>
	/// <remarks>Throws <see cref="InvalidOperationException"/> if no records are returned.</remarks>
	/// <seealso cref="QueryFirst{T}(Func{DbConnectorRecord, T})" />
	public ValueTask<T> QueryFirstAsync<T>(Func<DbConnectorRecord, T> map, CancellationToken cancellationToken = default) =>
		DoQueryFirstAsync(map ?? throw new ArgumentNullException(nameof(map)), single: false, orDefault: false, cancellationToken);

	/// <summary>
	/// Executes the query, converting the first record to the specified type.
	/// </summary>
	/// <remarks>Returns <c>default(T)</c> if no records are returned.</remarks>
	/// <seealso cref="QueryFirstOrDefault{T}()" />
	public ValueTask<T> QueryFirstOrDefaultAsync<T>(CancellationToken cancellationToken = default) =>
		DoQueryFirstAsync<T>(null, single: false, orDefault: true, cancellationToken);

	/// <summary>
	/// Executes the query, converting the first record to the specified type with the specified delegate.
	/// </summary>
	/// <remarks>Returns <c>default(T)</c> if no records are returned.</remarks>
	/// <seealso cref="QueryFirstOrDefault{T}(Func{DbConnectorRecord, T})" />
	public ValueTask<T> QueryFirstOrDefaultAsync<T>(Func<DbConnectorRecord, T> map, CancellationToken cancellationToken = default) =>
		DoQueryFirstAsync(map ?? throw new ArgumentNullException(nameof(map)), single: false, orDefault: true, cancellationToken);

	/// <summary>
	/// Executes the query, converting the first record to the specified type.
	/// </summary>
	/// <remarks>Throws <see cref="InvalidOperationException"/> if no records are returned, or if more than one record is returned.</remarks>
	/// <seealso cref="QuerySingle{T}()" />
	public ValueTask<T> QuerySingleAsync<T>(CancellationToken cancellationToken = default) =>
		DoQueryFirstAsync<T>(null, single: true, orDefault: false, cancellationToken);

	/// <summary>
	/// Executes the query, converting the first record to the specified type with the specified delegate.
	/// </summary>
	/// <remarks>Throws <see cref="InvalidOperationException"/> if no records are returned, or if more than one record is returned.</remarks>
	/// <seealso cref="QuerySingle{T}(Func{DbConnectorRecord, T})" />
	public ValueTask<T> QuerySingleAsync<T>(Func<DbConnectorRecord, T> map, CancellationToken cancellationToken = default) =>
		DoQueryFirstAsync(map ?? throw new ArgumentNullException(nameof(map)), single: true, orDefault: false, cancellationToken);

	/// <summary>
	/// Executes the query, converting the first record to the specified type.
	/// </summary>
	/// <remarks>Returns <c>default(T)</c> if no records are returned.
	/// Throws <see cref="InvalidOperationException"/> if more than one record is returned.</remarks>
	/// <seealso cref="QuerySingleOrDefault{T}()" />
	public ValueTask<T> QuerySingleOrDefaultAsync<T>(CancellationToken cancellationToken = default) =>
		DoQueryFirstAsync<T>(null, single: true, orDefault: true, cancellationToken);

	/// <summary>
	/// Executes the query, converting the first record to the specified type with the specified delegate.
	/// </summary>
	/// <remarks>Returns <c>default(T)</c> if no records are returned.
	/// Throws <see cref="InvalidOperationException"/> if more than one record is returned.</remarks>
	/// <seealso cref="QuerySingleOrDefault{T}(Func{DbConnectorRecord, T})" />
	public ValueTask<T> QuerySingleOrDefaultAsync<T>(Func<DbConnectorRecord, T> map, CancellationToken cancellationToken = default) =>
		DoQueryFirstAsync(map ?? throw new ArgumentNullException(nameof(map)), single: true, orDefault: true, cancellationToken);

	/// <summary>
	/// Executes the query, reading one record at a time and converting it to the specified type.
	/// </summary>
	/// <seealso cref="EnumerateAsync{T}(CancellationToken)" />
	public IEnumerable<T> Enumerate<T>() =>
		DoEnumerate<T>(null);

	/// <summary>
	/// Executes the query, reading one record at a time and converting it to the specified type with the specified delegate.
	/// </summary>
	/// <seealso cref="EnumerateAsync{T}(Func{DbConnectorRecord, T}, CancellationToken)" />
	public IEnumerable<T> Enumerate<T>(Func<DbConnectorRecord, T> map) =>
		DoEnumerate(map ?? throw new ArgumentNullException(nameof(map)));

	/// <summary>
	/// Executes the query, reading one record at a time and converting it to the specified type.
	/// </summary>
	/// <seealso cref="Enumerate{T}()" />
	public IAsyncEnumerable<T> EnumerateAsync<T>(CancellationToken cancellationToken = default) =>
		DoEnumerateAsync<T>(null, cancellationToken);

	/// <summary>
	/// Executes the query, reading one record at a time and converting it to the specified type with the specified delegate.
	/// </summary>
	/// <seealso cref="Enumerate{T}(Func{DbConnectorRecord, T})" />
	public IAsyncEnumerable<T> EnumerateAsync<T>(Func<DbConnectorRecord, T> map, CancellationToken cancellationToken = default) =>
		DoEnumerateAsync(map ?? throw new ArgumentNullException(nameof(map)), cancellationToken);

	/// <summary>
	/// Executes the query, preparing to read multiple result sets.
	/// </summary>
	/// <seealso cref="QueryMultipleAsync" />
	public DbConnectorResultSets QueryMultiple()
	{
		CreateCommand();
		Connector.SetActiveReader(Connector.ExecuteReaderCore());
		return new DbConnectorResultSets(Connector);
	}

	/// <summary>
	/// Executes the query, preparing to read multiple result sets.
	/// </summary>
	/// <seealso cref="QueryMultiple" />
	public async ValueTask<DbConnectorResultSets> QueryMultipleAsync(CancellationToken cancellationToken = default)
	{
		await CreateCommandAsync(cancellationToken).ConfigureAwait(false);
		Connector.SetActiveReader(await Connector.ExecuteReaderCoreAsync(cancellationToken).ConfigureAwait(false));
		return new DbConnectorResultSets(Connector);
	}

	/// <summary>
	/// Sets the timeout of the command.
	/// </summary>
	/// <remarks>Use <see cref="System.Threading.Timeout.InfiniteTimeSpan" /> (not <see cref="TimeSpan.Zero" />) for infinite timeout.</remarks>
	/// <exception cref="ArgumentOutOfRangeException"><c>timeSpan</c> is not positive or <see cref="System.Threading.Timeout.InfiniteTimeSpan" />.</exception>
	public DbConnectorCommand WithTimeout(TimeSpan timeSpan)
	{
		if (timeSpan <= TimeSpan.Zero && timeSpan != System.Threading.Timeout.InfiniteTimeSpan)
			throw new ArgumentOutOfRangeException(nameof(timeSpan), "Must be positive or 'Timeout.InfiniteTimeSpan'.");

		Timeout = timeSpan;
		return this;
	}

	public DbConnectorCommand WithParameter<T>(string key, T value) =>
		WithParameters(DbParameters.Create(key, value));

	public DbConnectorCommand WithParameters(DbParameters parameters)
	{
		m_parameters.Add(parameters);
		return this;
	}

	public DbConnectorCommand WithParameters(params IEnumerable<DbParameters> parameters) =>
		WithParameters(DbParameters.Create(parameters));

	public DbConnectorCommand WithParameters<T>(params IEnumerable<(string Name, T Value)> parameters) =>
		WithParameters(DbParameters.Create(parameters));

	public DbConnectorCommand WithParameters<T>(IEnumerable<KeyValuePair<string, T>> parameters) =>
		WithParameters(DbParameters.Create(parameters));

	public DbConnectorCommand WithParametersFromDto<T>(T dto, Func<string, bool>? where = null, Func<string, string>? renamed = null)
	{
		var parameters = DbParameters.FromDto(dto);
		if (where is not null)
			parameters = parameters.Where(where);
		if (renamed is not null)
			parameters = parameters.Renamed(renamed);
		return WithParameters(parameters);
	}

	/// <summary>
	/// Caches the command.
	/// </summary>
	public DbConnectorCommand Cache(bool cache = true)
	{
		IsCached = cache;
		return this;
	}

	/// <summary>
	/// Prepares the command.
	/// </summary>
	public DbConnectorCommand Prepare(bool prepare = true)
	{
		IsPrepared = prepare;
		return this;
	}

	internal DbConnectorCommand(DbConnector connector, string text, CommandType commandType)
	{
		Connector = connector;
		Text = text;
		CommandType = commandType;
		m_parameters = new DbParametersList();
	}

	private void Validate()
	{
		if (Connector is null)
			throw new InvalidOperationException("Use DbConnector to create commands.");
	}

	private DbCommandDisposer CreateCommand()
	{
		Validate();
		Connector.OpenConnection();
		DoCreateCommand(out var needsPrepare);
		if (needsPrepare)
			Connector.PrepareCommandCore();
		return new DbCommandDisposer(Connector);
	}

	private async ValueTask<DbCommandDisposer> CreateCommandAsync(CancellationToken cancellationToken = default)
	{
		Validate();
		await Connector.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
		DoCreateCommand(out var needsPrepare);
		if (needsPrepare)
			await Connector.PrepareCommandCoreAsync(cancellationToken).ConfigureAwait(false);
		return new DbCommandDisposer(Connector);
	}

	private void DoCreateCommand(out bool needsPrepare)
	{
		var commandText = Text;
		var commandType = CommandType;
		var timeout = Timeout;
		var parameters = Parameters;

		if (commandText.ContainsOrdinal("..."))
		{
			var nameValuePairs = Parameters.Enumerate().ToList();
			var index = 0;
			while (index < nameValuePairs.Count)
			{
				// look for @name... in SQL for collection parameters
				var (name, value) = nameValuePairs[index];
				if (!string.IsNullOrEmpty(name) && value is not string && value is not byte[] && value is IEnumerable list)
				{
					var itemCount = -1;
					var replacements = new List<(string Name, object? Value)>();

					string Replacement(Match match)
					{
						if (itemCount == -1)
						{
							itemCount = 0;

							foreach (var item in list)
							{
								replacements.Add(($"{name}_{itemCount}", item));
								itemCount++;
							}

							if (itemCount == 0)
								throw new InvalidOperationException($"Collection parameter '{name}' must not be empty.");
						}

						return string.Join(",", Enumerable.Range(0, itemCount).Select(x => $"{match.Groups[1]}_{x}"));
					}

					commandText = Regex.Replace(commandText, $@"([?@:]{Regex.Escape(name)})\.\.\.",
						Replacement, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

					// if special syntax wasn't found, leave the parameter alone, for databases that support collections directly
					if (itemCount != -1)
					{
						parameters = DbParameters.Create(nameValuePairs.Take(index).Concat(replacements).Concat(nameValuePairs.Skip(index + 1)));
						index += replacements.Count;
					}
					else
					{
						index += 1;
					}
				}
				else
				{
					index += 1;
				}
			}
		}

		IDbCommand? command;
		var transaction = Connector.Transaction;

		var wasCached = false;
		var isCached = false;
		var cache = IsCached ? Connector.CommandCache : null;
		if (cache is not null)
		{
			if (cache.TryGetCommand(commandText, out command))
			{
				wasCached = true;
			}
			else
			{
				command = CreateNewCommand();
				cache.AddCommand(commandText, command);
			}
			isCached = true;
		}
		else
		{
			command = CreateNewCommand();
		}

		Connector.SetActiveCommand(command, isCached);

		if (wasCached)
		{
			command.Transaction = transaction;

			var oldParameterCount = command.Parameters.Count;
			var newParameterCount = parameters.Reapply(Connector, startIndex: 0);
			if (oldParameterCount != newParameterCount)
				throw new InvalidOperationException($"Cached commands must always be executed with the same number of parameters (was {oldParameterCount}, now {newParameterCount}).");

			needsPrepare = false;
		}
		else
		{
			parameters.Apply(Connector);

			needsPrepare = IsPrepared;
		}

		IDbCommand CreateNewCommand()
		{
			var newCommand = Connector.CreateCommandCore();

			newCommand.CommandText = commandText;

			if (commandType != CommandType.Text)
				newCommand.CommandType = commandType;

			if (timeout is not null)
				newCommand.CommandTimeout = timeout == System.Threading.Timeout.InfiniteTimeSpan ? 0 : (int) Math.Ceiling(timeout.Value.TotalSeconds);

			if (transaction is not null)
				newCommand.Transaction = transaction;

			return newCommand;
		}
	}

	private IReadOnlyList<T> DoQuery<T>(Func<DbConnectorRecord, T>? map)
	{
		using var commandScope = CreateCommand();
		Connector.SetActiveReader(Connector.ExecuteReaderCore());
		using var readerScope = new DbReaderDisposer(Connector);
		var record = new DbConnectorRecord(Connector, new DbConnectorRecordState());

		var list = new List<T>();

		do
		{
			while (Connector.ReadReaderCore())
				list.Add(map is not null ? map(record) : record.Get<T>());
		}
		while (Connector.NextReaderResultCore());

		return list;
	}

	private async ValueTask<IReadOnlyList<T>> DoQueryAsync<T>(Func<DbConnectorRecord, T>? map, CancellationToken cancellationToken)
	{
		await using var commandScope = (await CreateCommandAsync(cancellationToken).ConfigureAwait(false)).ConfigureAwait(false);
		Connector.SetActiveReader(await Connector.ExecuteReaderCoreAsync(cancellationToken).ConfigureAwait(false));
		await using var readerScope = new DbReaderDisposer(Connector).ConfigureAwait(false);
		var record = new DbConnectorRecord(Connector, new DbConnectorRecordState());

		var list = new List<T>();

		do
		{
			while (await Connector.ReadReaderCoreAsync(cancellationToken).ConfigureAwait(false))
				list.Add(map is not null ? map(record) : record.Get<T>());
		}
		while (await Connector.NextReaderResultCoreAsync(cancellationToken).ConfigureAwait(false));

		return list;
	}

	private T DoQueryFirst<T>(Func<DbConnectorRecord, T>? map, bool single, bool orDefault)
	{
		using var commandScope = CreateCommand();
		Connector.SetActiveReader(single ? Connector.ExecuteReaderCore() : Connector.ExecuteReaderCore(CommandBehavior.SingleRow));
		using var readerScope = new DbReaderDisposer(Connector);

		while (!Connector.ReadReaderCore())
		{
			if (!Connector.NextReaderResultCore())
				return orDefault ? default(T)! : throw new InvalidOperationException("No records were found; use 'OrDefault' to permit this.");
		}

		var record = new DbConnectorRecord(Connector, state: null);
		var value = map is not null ? map(record) : record.Get<T>();

		if (single && Connector.ReadReaderCore())
			throw CreateTooManyRecordsException();

		if (single && Connector.NextReaderResultCore())
			throw CreateTooManyRecordsException();

		return value;
	}

	private async ValueTask<T> DoQueryFirstAsync<T>(Func<DbConnectorRecord, T>? map, bool single, bool orDefault, CancellationToken cancellationToken)
	{
		await using var commandScope = (await CreateCommandAsync(cancellationToken).ConfigureAwait(false)).ConfigureAwait(false);
		Connector.SetActiveReader(single ? await Connector.ExecuteReaderCoreAsync(cancellationToken).ConfigureAwait(false) : await Connector.ExecuteReaderCoreAsync(CommandBehavior.SingleRow, cancellationToken).ConfigureAwait(false));
		await using var readerScope = new DbReaderDisposer(Connector).ConfigureAwait(false);

		while (!await Connector.ReadReaderCoreAsync(cancellationToken).ConfigureAwait(false))
		{
			if (!await Connector.NextReaderResultCoreAsync(cancellationToken).ConfigureAwait(false))
				return orDefault ? default(T)! : throw CreateNoRecordsException();
		}

		var record = new DbConnectorRecord(Connector, state: null);
		var value = map is not null ? map(record) : record.Get<T>();

		if (single && await Connector.ReadReaderCoreAsync(cancellationToken).ConfigureAwait(false))
			throw CreateTooManyRecordsException();

		if (single && await Connector.NextReaderResultCoreAsync(cancellationToken).ConfigureAwait(false))
			throw CreateTooManyRecordsException();

		return value;
	}

	private static InvalidOperationException CreateNoRecordsException() => new("No records were found; use 'OrDefault' to permit this.");

	private static InvalidOperationException CreateTooManyRecordsException() => new("Additional records were found; use 'First' to permit this.");

	private IEnumerable<T> DoEnumerate<T>(Func<DbConnectorRecord, T>? map)
	{
		using var commandScope = CreateCommand();
		Connector.SetActiveReader(Connector.ExecuteReaderCore());
		using var readerScope = new DbReaderDisposer(Connector);
		var record = new DbConnectorRecord(Connector, new DbConnectorRecordState());

		do
		{
			while (Connector.ReadReaderCore())
				yield return map is not null ? map(record) : record.Get<T>();
		}
		while (Connector.NextReaderResultCore());
	}

	private async IAsyncEnumerable<T> DoEnumerateAsync<T>(Func<DbConnectorRecord, T>? map, [EnumeratorCancellation] CancellationToken cancellationToken)
	{
		await using var commandScope = (await CreateCommandAsync(cancellationToken).ConfigureAwait(false)).ConfigureAwait(false);
		Connector.SetActiveReader(await Connector.ExecuteReaderCoreAsync(cancellationToken).ConfigureAwait(false));
		await using var readerScope = new DbReaderDisposer(Connector).ConfigureAwait(false);
		var record = new DbConnectorRecord(Connector, new DbConnectorRecordState());

		do
		{
			while (await Connector.ReadReaderCoreAsync(cancellationToken).ConfigureAwait(false))
				yield return map is not null ? map(record) : record.Get<T>();
		}
		while (await Connector.NextReaderResultCoreAsync(cancellationToken).ConfigureAwait(false));
	}

	private readonly DbParametersList m_parameters;
}
