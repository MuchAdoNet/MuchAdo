using System.Data;
using BenchmarkDotNet.Attributes;
using MuchAdo;
using MuchAdo.SqlFormatting;
#if SQLSERVER
using Microsoft.Data.SqlClient;
#endif
using Microsoft.Data.Sqlite;
#if MYSQL
using MySqlConnector;
#endif
#if NGPSQL
using Npgsql;
#endif

namespace Benchmarks;

public abstract class PreparedCachedBenchmark : IDisposable
{
	public class SqlitePreparedCachedBenchmark : PreparedCachedBenchmark
	{
		public SqlitePreparedCachedBenchmark()
			: base(new SqliteConnection("Data Source=:memory:"),
				columnsSql: "ItemId integer primary key, Value integer not null",
				recordCount: 10000)
		{
		}
	}

#if MYSQL
	public class MySqlPreparedCachedBenchmark : PreparedCachedBenchmark
	{
		public MySqlPreparedCachedBenchmark()
			: base(new MySqlConnection("Server=localhost;User Id=root;Password=test;SSL Mode=none;Database=test;Ignore Prepare=false"),
				columnsSql: "ItemId int not null auto_increment primary key, Value int not null",
				recordCount: 500)
		{
		}
	}
#endif

#if SQLSERVER
	public class SqlServerPreparedCachedBenchmark : PreparedCachedBenchmark
	{
		public SqlServerPreparedCachedBenchmark()
			: base(new SqlConnection("data source=localhost;user id=sa;password=P@ssw0rd;initial catalog=test"),
				columnsSql: "ItemId int not null identity primary key, Value int not null",
				recordCount: 250,
				createParameter: x => new SqlParameter { Value = x, DbType = DbType.Int32 })
		{
		}
	}
#endif

#if NGPSQL
	public class NpgsqlPreparedCachedBenchmark : PreparedCachedBenchmark
	{
		public NpgsqlPreparedCachedBenchmark()
			: base(new NpgsqlConnection("host=localhost;user id=root;password=test;database=test"),
				columnsSql: "ItemId serial primary key, Value int not null",
				recordCount: 500)
		{
		}
	}
#endif

	protected PreparedCachedBenchmark(IDbConnection connection, string columnsSql, int recordCount, Func<int, object>? createParameter = null)
	{
		m_connector = new DbConnector(connection);
		m_connector.Command("drop table if exists PreparedCachedBenchmark; ").Execute();
		m_connector.CommandFormat($"create table PreparedCachedBenchmark ({Sql.Raw(columnsSql)});").Execute();
		m_recordCount = recordCount;
		m_createParameter = createParameter;

		m_paramCount = 100;
		m_sql = $"insert into PreparedCachedBenchmark (Value) values ({string.Join(" + ", Enumerable.Range(0, m_paramCount).Select(x => $"@Value{x}"))});";
	}

	[Benchmark]
	public void Normal()
	{
		for (var i = 0; i < m_recordCount; i++)
			m_connector.Command(m_sql).WithParameters(Params(i)).Execute();
	}

	[Benchmark]
	public void Cached()
	{
		for (var i = 0; i < m_recordCount; i++)
			m_connector.Command(m_sql).WithParameters(Params(i)).Cache().Execute();
	}

	[Benchmark]
	public void Prepared()
	{
		for (var i = 0; i < m_recordCount; i++)
			m_connector.Command(m_sql).WithParameters(Params(i)).Prepare().Execute();
	}

	[Benchmark]
	public void PreparedAndCached()
	{
		for (var i = 0; i < m_recordCount; i++)
			m_connector.Command(m_sql).WithParameters(Params(i)).Prepare().Cache().Execute();
	}

	public void Dispose() => m_connector.Dispose();

	private DbParametersList Params(int i) => new(Enumerable.Range(0, m_paramCount).Select(x => DbParameters.Create($"Value{x}", (object?) Param(i + x))));

	private object Param(int x) => m_createParameter is null ? x : m_createParameter(x);

	private readonly DbConnector m_connector;
	private readonly int m_recordCount;
	private readonly Func<int, object>? m_createParameter;
	private readonly int m_paramCount;
	private readonly string m_sql;
}
