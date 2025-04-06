using System.Text;
using BenchmarkDotNet.Attributes;
using Microsoft.Data.Sqlite;
using MuchAdo;
using MuchAdo.BulkInsert;

namespace Benchmarks;

[MemoryDiagnoser]
public class DataMapperBenchmark : IDisposable
{
	protected DataMapperBenchmark()
	{
		m_connector = new DbConnector(new SqliteConnection("Data Source=:memory:"));
		m_connector.Command("drop table if exists DataMapperBenchmark;").Execute();
		m_connector.Command("create table DataMapperBenchmark (ItemId integer primary key, AnInteger integer, AReal real, AString text, ABlob blob);").Execute();

		const int recordCount = 10000;
		m_connector
			.Command("insert into DataMapperBenchmark (AnInteger, AReal, AString, ABlob) values (@AnInteger, @AReal, @AString, @ABlob)...;")
			.BulkInsert(Enumerable.Range(0, recordCount)
				.Select(x => new DbParametersList(
					DbParameters.Create("AnInteger", x < recordCount ? x : (int?) null),
					DbParameters.Create("AReal", x < recordCount ? 1.0 / (x + 1.0) : (double?) null),
					DbParameters.Create("AString", x < recordCount ? $"{x:0000}" : null),
					DbParameters.Create("ABlob", x < recordCount ? Encoding.UTF8.GetBytes($"{x:0000}") : null))));
	}

	[Benchmark]
	public long Int64() => m_connector.Command("select AnInteger from DataMapperBenchmark where AnInteger is not null;").Enumerate<long>().Last();

	[Benchmark]
	public long? NullableInt64() => m_connector.Command("select AnInteger from DataMapperBenchmark;").Enumerate<long?>().Last();

	[Benchmark]
	public double Double() => m_connector.Command("select AReal from DataMapperBenchmark where AReal is not null;").Enumerate<double>().Last();

	[Benchmark]
	public double? NullableDouble() => m_connector.Command("select AReal from DataMapperBenchmark;").Enumerate<double?>().Last();

	[Benchmark]
	public StringSplitOptions Enum() => m_connector.Command("select AnInteger from DataMapperBenchmark where AnInteger is not null;").Enumerate<StringSplitOptions>().Last();

	[Benchmark]
	public StringSplitOptions? NullableEnum() => m_connector.Command("select AnInteger from DataMapperBenchmark;").Enumerate<StringSplitOptions?>().Last();

	[Benchmark]
	public string? String() => m_connector.Command("select AString from DataMapperBenchmark;").Enumerate<string?>().Last();

	[Benchmark]
	public byte[]? Blob() => m_connector.Command("select ABlob from DataMapperBenchmark;").Enumerate<byte[]?>().Last();

	[Benchmark]
	public (long? AnInteger, double AReal) NullableInt64AndDouble() => m_connector.Command("select AnInteger, ifnull(AReal, 0.0) from DataMapperBenchmark;").Enumerate<(long?, double)>().Last();

	[Benchmark]
	public MyTuple MyTuples() => m_connector.Command("select AnInteger, ifnull(AReal, 0.0) from DataMapperBenchmark;").Enumerate<MyTuple>().Last();

	public void Dispose() => m_connector.Dispose();

	public readonly record struct MyTuple(long? AnInteger, double AReal);

	private readonly DbConnector m_connector;
}
