using System.Data;
using Microsoft.Data.SqlClient;

namespace MuchAdo.SqlServer;

/// <summary>
/// A <see cref="DbConnector" /> optimized for SqlConnector.
/// </summary>
public class SqlServerDbConnector : DbConnector
{
	public SqlServerDbConnector(SqlConnection connection)
		: this(connection, SqlServerDbConnectorSettings.Default)
	{
	}

	public SqlServerDbConnector(SqlConnection connection, SqlServerDbConnectorSettings settings)
		: base(connection, settings)
	{
	}

	public new SqlConnection Connection => (SqlConnection) base.Connection;

	public new SqlTransaction? Transaction => (SqlTransaction?) base.Transaction;

	public new SqlCommand? ActiveCommand => (SqlCommand?) base.ActiveCommand;

#if NET
	public new SqlBatch? ActiveBatch => ActiveCommandOrBatch as SqlBatch;
#endif

	public new SqlDataReader? ActiveReader => (SqlDataReader?) base.ActiveReader;

	public new SqlConnection GetOpenConnection() => (SqlConnection) base.GetOpenConnection();

	public new ValueTask<SqlConnection> GetOpenConnectionAsync(CancellationToken cancellationToken = default)
	{
		var task = base.GetOpenConnectionAsync(cancellationToken);
		return task.IsCompletedSuccessfully ? new ValueTask<SqlConnection>((SqlConnection) task.Result) : DoAsync(task);
		static async ValueTask<SqlConnection> DoAsync(ValueTask<IDbConnection> t) => (SqlConnection) await t.ConfigureAwait(false);
	}

	protected override IDataParameter CreateParameterCore<T>(string name, T value) => new SqlParameter(name, value is null ? DBNull.Value : value);
}
