using System.Data;
using MySqlConnector;

namespace MuchAdo.MySql;

public class MySqlDbConnector : DbConnector
{
	public MySqlDbConnector(MySqlConnection connection, MySqlDbConnectorSettings? settings = null)
		: base(connection, settings)
	{
	}

	public new MySqlConnection Connection => (MySqlConnection) base.Connection;

	public new MySqlTransaction? Transaction => (MySqlTransaction?) base.Transaction;

	internal new MySqlCommand? ActiveCommand => (MySqlCommand?) base.ActiveCommand;

	internal new MySqlDataReader? ActiveReader => (MySqlDataReader?) base.ActiveReader;

	public new MySqlConnection GetOpenConnection() => (MySqlConnection) base.GetOpenConnection();

	public new ValueTask<MySqlConnection> GetOpenConnectionAsync(CancellationToken cancellationToken = default)
	{
		var task = base.GetOpenConnectionAsync(cancellationToken);
		return task.IsCompletedSuccessfully ? new ValueTask<MySqlConnection>((MySqlConnection) task.Result) : DoAsync(task);
		static async ValueTask<MySqlConnection> DoAsync(ValueTask<IDbConnection> t) => (MySqlConnection) await t.ConfigureAwait(false);
	}

#if NETSTANDARD2_0
	protected override ValueTask CloseConnectionCoreAsync() => new(Connection.CloseAsync());

	protected override ValueTask DisposeConnectionCoreAsync() => new(Connection.DisposeAsync());

	protected override async ValueTask<IDbTransaction> BeginTransactionCoreAsync(CancellationToken cancellationToken) =>
		await Connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

	protected override async ValueTask<IDbTransaction> BeginTransactionCoreAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken) =>
		await Connection.BeginTransactionAsync(isolationLevel, cancellationToken).ConfigureAwait(false);

	protected override ValueTask CommitTransactionCoreAsync(CancellationToken cancellationToken) => new(Transaction!.CommitAsync(cancellationToken));

	protected override ValueTask RollbackTransactionCoreAsync(CancellationToken cancellationToken) => new(Transaction!.RollbackAsync(cancellationToken));

	protected override ValueTask DisposeTransactionCoreAsync() => new(Transaction!.DisposeAsync());

	protected override ValueTask PrepareCommandCoreAsync(CancellationToken cancellationToken) => new(ActiveCommand!.PrepareAsync(cancellationToken));

	protected override ValueTask DisposeCommandCoreAsync() => new(ActiveCommand!.DisposeAsync());

	protected override ValueTask DisposeReaderCoreAsync() => new(ActiveReader!.DisposeAsync());
#endif
}
