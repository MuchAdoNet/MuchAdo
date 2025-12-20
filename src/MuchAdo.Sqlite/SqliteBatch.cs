using System.Data;
using Microsoft.Data.Sqlite;

namespace MuchAdo.Sqlite;

/// <summary>
/// Emulates a multi-command batch for Microsoft.Data.Sqlite, which doesn't support <c>DbBatch</c>/<c>CreateBatch</c>.
/// </summary>
internal sealed class SqliteBatch
{
	public SqliteBatch(SqliteConnection connection) => Connection = connection;

	public SqliteConnection Connection { get; }

	public List<SqliteCommand> Commands { get; } = [];

	public SqliteCommand AddCommand(CommandType commandType)
	{
		var cmd = Connection.CreateCommand();
		if (commandType != CommandType.Text)
			cmd.CommandType = commandType;
		Commands.Add(cmd);
		return cmd;
	}

	public void SetTimeout(int timeout)
	{
		foreach (var cmd in Commands)
			cmd.CommandTimeout = timeout;
	}

	public void SetTransaction(SqliteTransaction? transaction)
	{
		foreach (var cmd in Commands)
			cmd.Transaction = transaction;
	}

	public void Prepare()
	{
		foreach (var cmd in Commands)
			cmd.Prepare();
	}

	public ValueTask PrepareAsync(CancellationToken cancellationToken)
	{
		if (Commands.Count == 0)
			return default;

#if NET
		return DoAsync();

		async ValueTask DoAsync()
		{
			foreach (var cmd in Commands)
				await cmd.PrepareAsync(cancellationToken).ConfigureAwait(false);
		}
#else
		Prepare();
		return default;
#endif
	}

	public void Cancel()
	{
		foreach (var cmd in Commands)
			cmd.Cancel();
	}

	public void Dispose()
	{
		foreach (var cmd in Commands)
			cmd.Dispose();
		Commands.Clear();
	}

	public async ValueTask DisposeAsync()
	{
#if NET
		foreach (var cmd in Commands)
			await cmd.DisposeAsync().ConfigureAwait(false);
#else
		foreach (var cmd in Commands)
			cmd.Dispose();
#endif
		Commands.Clear();
	}
}
