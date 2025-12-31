using System.Data;
using Microsoft.Data.Sqlite;

namespace MuchAdo.Sqlite;

/// <summary>
/// Emulates a multi-command batch for Microsoft.Data.Sqlite, which doesn't support <c>DbBatch</c>/<c>CreateBatch</c>.
/// </summary>
internal sealed class SqliteBatch(SqliteConnection connection)
{
	public SqliteConnection Connection { get; } = connection;

	public List<SqliteCommand> Commands { get; } = [];

	public SqliteCommand AddCommand(CommandType commandType)
	{
		var command = Connection.CreateCommand();
		if (commandType != CommandType.Text)
			command.CommandType = commandType;
		Commands.Add(command);
		return command;
	}

	public void SetTimeout(int timeout)
	{
		foreach (var command in Commands)
			command.CommandTimeout = timeout;
	}

	public void SetTransaction(SqliteTransaction? transaction)
	{
		foreach (var command in Commands)
			command.Transaction = transaction;
	}

	public void Prepare()
	{
		foreach (var command in Commands)
			command.Prepare();
	}

	public ValueTask PrepareAsync(CancellationToken cancellationToken)
	{
		if (Commands.Count == 0)
			return default;

#if NET
		return DoAsync();

		async ValueTask DoAsync()
		{
			foreach (var command in Commands)
				await command.PrepareAsync(cancellationToken).ConfigureAwait(false);
		}
#else
		Prepare();
		return default;
#endif
	}

	public void Cancel()
	{
		foreach (var command in Commands)
			command.Cancel();
	}

	public void Dispose()
	{
		foreach (var command in Commands)
			command.Dispose();
		Commands.Clear();
	}

	public async ValueTask DisposeAsync()
	{
#if NET
		foreach (var command in Commands)
			await command.DisposeAsync().ConfigureAwait(false);
#else
		foreach (var command in Commands)
			command.Dispose();
#endif
		Commands.Clear();
	}
}
