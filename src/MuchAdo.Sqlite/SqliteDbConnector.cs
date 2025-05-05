using System.Data;
using Microsoft.Data.Sqlite;

namespace MuchAdo.Sqlite;

/// <summary>
/// A <see cref="DbConnector" /> optimized for Microsoft.Data.Sqlite.
/// </summary>
public class SqliteDbConnector : DbConnector
{
	public SqliteDbConnector(SqliteConnection connection)
		: this(connection, SqliteDbConnectorSettings.Default)
	{
	}

	public SqliteDbConnector(SqliteConnection connection, SqliteDbConnectorSettings settings)
		: base(connection, settings)
	{
	}

	public new SqliteConnection Connection => (SqliteConnection) base.Connection;

	public new SqliteTransaction? Transaction => (SqliteTransaction?) base.Transaction;

	public new SqliteCommand? ActiveCommand => (SqliteCommand?) base.ActiveCommand;

	public new SqliteDataReader? ActiveReader => (SqliteDataReader?) base.ActiveReader;

	public DbTransactionDisposer BeginTransaction(bool deferred) =>
		AttachTransaction(GetOpenConnection().BeginTransaction(deferred));

	public DbTransactionDisposer BeginTransaction(IsolationLevel isolationLevel, bool deferred) =>
		AttachTransaction(GetOpenConnection().BeginTransaction(isolationLevel, deferred));

	public new SqliteConnection GetOpenConnection() => (SqliteConnection) base.GetOpenConnection();

	public new ValueTask<SqliteConnection> GetOpenConnectionAsync(CancellationToken cancellationToken = default)
	{
		var task = base.GetOpenConnectionAsync(cancellationToken);
		return task.IsCompletedSuccessfully ? new ValueTask<SqliteConnection>((SqliteConnection) task.Result) : DoAsync(task);
		static async ValueTask<SqliteConnection> DoAsync(ValueTask<IDbConnection> t) => (SqliteConnection) await t.ConfigureAwait(false);
	}

	protected override IDataParameter CreateParameterCore<T>(string name, T value) => new SqliteParameter(name, value);
}
