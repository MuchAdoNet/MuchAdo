using System.Data;
using System.Data.Common;
using Npgsql;

namespace MuchAdo.Npgsql;

/// <summary>
/// A <see cref="DbConnector" /> optimized for Npgsql.
/// </summary>
public class NpgsqlDbConnector : DbConnector
{
	public NpgsqlDbConnector(DbConnection connection)
		: this(connection, NpgsqlDbConnectorSettings.Default)
	{
	}

	public NpgsqlDbConnector(DbConnection connection, NpgsqlDbConnectorSettings settings)
		: base(connection, settings)
	{
		if (connection is not NpgsqlConnection)
			throw new ArgumentException("The connection must be a NpgsqlConnection.", nameof(connection));
	}

	public new NpgsqlConnection Connection => (NpgsqlConnection) base.Connection;

	public new NpgsqlTransaction? Transaction => (NpgsqlTransaction?) base.Transaction;

	public new NpgsqlCommand? ActiveCommand => (NpgsqlCommand?) base.ActiveCommand;

#if !NET
	public NpgsqlBatch? ActiveBatch => ActiveCommandOrBatch as NpgsqlBatch;
#else
	public new NpgsqlBatch? ActiveBatch => ActiveCommandOrBatch as NpgsqlBatch;
#endif

	public new NpgsqlDataReader? ActiveReader => (NpgsqlDataReader?) base.ActiveReader;

	public new NpgsqlConnection GetOpenConnection() => (NpgsqlConnection) base.GetOpenConnection();

	public new ValueTask<NpgsqlConnection> GetOpenConnectionAsync(CancellationToken cancellationToken = default)
	{
		var task = base.GetOpenConnectionAsync(cancellationToken);
		return task.IsCompletedSuccessfully ? new ValueTask<NpgsqlConnection>((NpgsqlConnection) task.Result) : DoAsync(task);
		static async ValueTask<NpgsqlConnection> DoAsync(ValueTask<IDbConnection> t) => (NpgsqlConnection) await t.ConfigureAwait(false);
	}

	protected override IDataParameter CreateParameterCore<T>(string name, T value) => new NpgsqlParameter<T>(name, value);

	protected override void SetParameterValueCore<T>(IDataParameter parameter, T value)
	{
		if (parameter is NpgsqlParameter<T> npgsqlParameter)
			npgsqlParameter.TypedValue = value;
		else
			base.SetParameterValueCore(parameter, value);
	}

#if !NET
	protected override ValueTask CloseConnectionCoreAsync() => new(Connection.CloseAsync());

	protected override ValueTask DisposeConnectionCoreAsync() => Connection.DisposeAsync();

	protected override ValueTask CommitTransactionCoreAsync(CancellationToken cancellationToken) => new(Transaction!.CommitAsync(cancellationToken));

	protected override ValueTask RollbackTransactionCoreAsync(CancellationToken cancellationToken) => new(Transaction!.RollbackAsync(cancellationToken));

	protected override ValueTask DisposeTransactionCoreAsync() => Transaction!.DisposeAsync();

	protected override int ExecuteNonQueryCore()
	{
		if (ActiveBatch is { } batch)
			return batch.ExecuteNonQuery();

		return base.ExecuteNonQueryCore();
	}

	protected override ValueTask<int> ExecuteNonQueryCoreAsync(CancellationToken cancellationToken)
	{
		if (ActiveBatch is { } batch)
			return new ValueTask<int>(batch.ExecuteNonQueryAsync(cancellationToken));

		return base.ExecuteNonQueryCoreAsync(cancellationToken);
	}

	protected override IDataReader ExecuteReaderCore()
	{
		if (ActiveBatch is { } batch)
			return batch.ExecuteReader();

		return base.ExecuteReaderCore();
	}

	protected override async ValueTask<IDataReader> ExecuteReaderCoreAsync(CancellationToken cancellationToken)
	{
		if (ActiveBatch is { } batch)
			return await batch.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

		return await base.ExecuteReaderCoreAsync(cancellationToken).ConfigureAwait(false);
	}

	protected override IDataReader ExecuteReaderCore(CommandBehavior commandBehavior)
	{
		if (ActiveBatch is { } batch)
			return batch.ExecuteReader(commandBehavior);

		return base.ExecuteReaderCore(commandBehavior);
	}

	protected override async ValueTask<IDataReader> ExecuteReaderCoreAsync(CommandBehavior commandBehavior, CancellationToken cancellationToken)
	{
		if (ActiveBatch is { } batch)
			return await batch.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

		return await base.ExecuteReaderCoreAsync(commandBehavior, cancellationToken).ConfigureAwait(false);
	}

	protected override void PrepareCore()
	{
		if (ActiveBatch is { } batch)
			batch.Prepare();
		else
			base.PrepareCore();
	}

	protected override ValueTask PrepareCoreAsync(CancellationToken cancellationToken)
	{
		if (ActiveBatch is { } batch)
			return new ValueTask(batch.PrepareAsync(cancellationToken));

		return base.PrepareCoreAsync(cancellationToken);
	}

	protected override void CancelCore()
	{
		if (ActiveBatch is { } batch)
			batch.Cancel();
		else
			base.CancelCore();
	}

	protected override void DisposeCommandOrBatchCore()
	{
		if (ActiveBatch is { } batch)
			batch.Dispose();
		else
			base.DisposeCommandOrBatchCore();
	}

	protected override object CreateBatchCore() => Connection.CreateBatch();

	protected override void AddBatchCommandCore(CommandType commandType)
	{
		if (ActiveBatch is { } batch)
		{
			var command = new NpgsqlBatchCommand();
			if (commandType != CommandType.Text)
				command.CommandType = commandType;
			batch.BatchCommands.Add(command);
			return;
		}

		base.AddBatchCommandCore(commandType);
	}

	protected override void SetTimeoutCore(int timeout)
	{
		if (ActiveCommandOrBatch is NpgsqlBatch dbBatch)
			dbBatch.Timeout = timeout;
		else
			base.SetTimeoutCore(timeout);
	}

	protected override void SetTransactionCore(IDbTransaction? transaction)
	{
		if (ActiveCommandOrBatch is NpgsqlBatch dbBatch && transaction is NpgsqlTransaction dbTransaction)
			dbBatch.Transaction = dbTransaction;
		else
			base.SetTransactionCore(transaction);
	}

	protected override void SetCommandTextCore(int commandIndex, string commandText)
	{
		if (ActiveCommandOrBatch is NpgsqlBatch dbBatch)
			dbBatch.BatchCommands[commandIndex].CommandText = commandText;
		else
			base.SetCommandTextCore(commandIndex, commandText);
	}

	protected override IDataParameterCollection GetParameterCollectionCore(int commandIndex)
	{
		if (ActiveCommandOrBatch is NpgsqlBatch dbBatch)
			return dbBatch.BatchCommands[commandIndex].Parameters;

		return base.GetParameterCollectionCore(commandIndex);
	}

	protected override ValueTask DisposeReaderCoreAsync() => ActiveReader!.DisposeAsync();
#endif
}
