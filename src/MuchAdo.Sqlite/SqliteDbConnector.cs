using System.Data;
using System.Data.Common;
using Microsoft.Data.Sqlite;

namespace MuchAdo.Sqlite;

/// <summary>
/// A <see cref="DbConnector" /> optimized for Microsoft.Data.Sqlite.
/// </summary>
public class SqliteDbConnector : DbConnector
{
	public SqliteDbConnector(DbConnection connection)
		: this(connection, SqliteDbConnectorSettings.Default)
	{
	}

	public SqliteDbConnector(DbConnection connection, SqliteDbConnectorSettings settings)
		: base(connection, settings)
	{
		if (connection is not SqliteConnection)
			throw new ArgumentException("The connection must be a SqliteConnection.", nameof(connection));
	}

	public new SqliteConnection Connection => (SqliteConnection) base.Connection;

	public new SqliteTransaction? Transaction => (SqliteTransaction?) base.Transaction;

	public new SqliteCommand? ActiveCommand => (SqliteCommand?) base.ActiveCommand;

	public new SqliteDataReader? ActiveReader => (SqliteDataReader?) base.ActiveReader;

	public new SqliteConnection GetOpenConnection() => (SqliteConnection) base.GetOpenConnection();

	public new ValueTask<SqliteConnection> GetOpenConnectionAsync(CancellationToken cancellationToken = default)
	{
		var task = base.GetOpenConnectionAsync(cancellationToken);
		return task.IsCompletedSuccessfully ? new ValueTask<SqliteConnection>((SqliteConnection) task.Result) : DoAsync(task);
		static async ValueTask<SqliteConnection> DoAsync(ValueTask<IDbConnection> t) => (SqliteConnection) await t.ConfigureAwait(false);
	}

	protected override IDbTransaction BeginTransactionCore(DbTransactionSettings settings)
	{
		if (settings is SqliteDbTransactionSettings sqliteSettings)
		{
			return sqliteSettings.IsolationLevel is { } isolationLevel
				? Connection.BeginTransaction(isolationLevel, sqliteSettings.IsDeferred)
				: Connection.BeginTransaction(sqliteSettings.IsDeferred);
		}

		return base.BeginTransactionCore(settings);
	}

	protected override ValueTask<IDbTransaction> BeginTransactionCoreAsync(DbTransactionSettings settings, CancellationToken cancellationToken) =>
		new(BeginTransactionCore(settings));

	protected override IDataParameter CreateParameterCore<T>(string name, T value) => new SqliteParameter(name, value);
}
