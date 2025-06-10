using System.Data;
using System.Diagnostics;
using FluentAssertions;
using MySqlConnector;
using NUnit.Framework;
using static FluentAssertions.FluentActions;

namespace MuchAdo.MySql.Tests;

[TestFixture(Explicit = true)]
internal sealed class MySqlTests
{
	[Test]
	public async Task PrepareCacheTests()
	{
		var tableName = Sql.Name($"{nameof(PrepareCacheTests)}_{c_framework}");

		await using var connector = CreateConnector();
		await connector
			.CommandFormat($"drop table if exists {tableName}")
			.CommandFormat($"create table {tableName} (Id int not null auto_increment primary key, Name varchar(100) not null)")
			.ExecuteAsync();

		var insertSql = Sql.Format($"insert into {tableName} (Name) values (@itemA); insert into {tableName} (Name) values (@itemB);");
		(await connector.Command(insertSql, Sql.NamedParams(("itemA", "one"), ("itemB", "two"))).Prepare().Cache().ExecuteAsync()).Should().Be(2);
		(await connector.Command(insertSql, Sql.NamedParams(("itemA", "three"), ("itemB", "four"))).Prepare().Cache().ExecuteAsync()).Should().Be(2);

		await Invoking(async () => await connector.Command(insertSql, Sql.NamedParams(("itemA", "five"), ("itemB", "six"), ("itemC", "seven"))).Prepare().Cache().ExecuteAsync()).Should().ThrowAsync<InvalidOperationException>();
		await Invoking(async () => await connector.Command(insertSql, Sql.NamedParam("itemA", "five")).Prepare().Cache().ExecuteAsync()).Should().ThrowAsync<InvalidOperationException>();
		await Invoking(async () => await connector.Command(insertSql, Sql.NamedParams(("itemB", "six"), ("itemA", "five"))).Prepare().Cache().ExecuteAsync()).Should().ThrowAsync<InvalidOperationException>();

		(await connector.CommandFormat($"select Name from {tableName} order by Id").QueryAsync<string>()).Should().Equal("one", "two", "three", "four");
	}

	[TestCase(false)]
	[TestCase(true)]
	public async Task CancelTest(bool autoCancel)
	{
		var tableName = Sql.Name($"{nameof(CancelTest)}_{autoCancel}_{c_framework}");

		await using var connector = CreateConnector(cancelUnfinishedCommands: autoCancel);

		var stopwatch = Stopwatch.StartNew();
		await foreach (var value in connector
			.CommandFormat($"drop table if exists {tableName}")
			.CommandFormat($"create table {tableName} (Id int not null auto_increment primary key, Value int not null)")
			.CommandFormat($"insert into {tableName} (Value) values {Sql.List(Enumerable.Range(1, 100).Select(x => Sql.Format($"({x})")))}")
			.CommandFormat($"select t1.Value from {tableName} t1 join {tableName} t2 join {tableName} t3 join {tableName} t4")
			.EnumerateAsync<int>())
		{
			if (!autoCancel)
				connector.Cancel();
			break;
		}

		// without cancel, disposing the reader still has to wait for the many rows to be downloaded
		stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(5));
	}

	[Test]
	public async Task SprocInOutTest()
	{
		var sprocName = $"{nameof(SprocInOutTest)}_{c_framework}";

		await using var connector = CreateConnector();
		await connector
			.CommandFormat($"drop procedure if exists {Sql.Name(sprocName)}")
			.CommandFormat($"create procedure {Sql.Name(sprocName)} (inout Value int) begin set Value = Value * Value; end")
			.ExecuteAsync();

		var param = new MySqlParameter("Value", MySqlDbType.Int32) { Direction = ParameterDirection.InputOutput, Value = 11 };
		await connector.StoredProcedure(sprocName, param).ExecuteAsync();
		param.Value.Should().Be(121);
	}

	[Test]
	public async Task SprocInTest()
	{
		var sprocName = $"{nameof(SprocInTest)}_{c_framework}";

		await using var connector = CreateConnector();
		await connector
			.CommandFormat($"drop procedure if exists {Sql.Name(sprocName)}")
			.CommandFormat($"create procedure {Sql.Name(sprocName)} (in Value int) begin select Value, Value * Value; end")
			.ExecuteAsync();

		(await connector.StoredProcedure(sprocName, Sql.NamedParam("Value", 11)).QuerySingleAsync<(int, long)>()).Should().Be((11, 121));
	}

	[Test]
	public async Task UnnamedParameterTest()
	{
		var tableName = Sql.Name($"{nameof(UnnamedParameterTest)}_{c_framework}");

		await using var connector = CreateConnector();

		var lastCommandText = "";
		connector.Executing += (_, e) => lastCommandText = e.CommandBatch.GetCommand(e.CommandBatch.CommandCount - 1).BuildText(connector.SqlSyntax);

		await connector
			.CommandFormat($"drop table if exists {tableName}")
			.CommandFormat($"create table {tableName} (Id int not null auto_increment primary key, Name varchar(100) not null)")
			.CommandFormat($"insert into {tableName} (Name) values (?), (?)", Sql.Param("one"), Sql.Param("two"))
			.ExecuteAsync();

		var three = Sql.Param("three");
		var four = "four";
		await connector.CommandFormat($"insert into {tableName} (Name) values ({three}), ({four}), ({three}), ({four})").ExecuteAsync();
		lastCommandText.Should().Contain("(Name) values (?), (?), (?), (?)");

		(await connector.CommandFormat($"select Name from {tableName} order by Id").QueryAsync<string>()).Should().Equal("one", "two", "three", "four", "three", "four");
	}

	[Test]
	public async Task MySqlDecimalTest()
	{
		var tableName = Sql.Name($"{nameof(MySqlDecimalTest)}_{c_framework}");

		await using var connector = CreateConnector();

		await connector
			.CommandFormat($"drop table if exists {tableName}")
			.CommandFormat($"create table {tableName} (Id int not null auto_increment primary key, Value decimal(10, 2) not null)")
			.CommandFormat($"insert into {tableName} (Value) values (?)", Sql.Param(6.875m))
			.ExecuteAsync();

		(await connector.Command(Sql.Format($"select Value from {tableName}")).QuerySingleAsync<decimal>()).Should().Be(6.88m);
		(await connector.Command(Sql.Format($"select Value from {tableName}")).QuerySingleAsync<MySqlDecimal>()).Value.Should().Be(6.88m);
	}

	private static MySqlDbConnector CreateConnector(bool cancelUnfinishedCommands = false) => new(
		new MySqlConnection("Server=localhost;User Id=root;Password=test;SSL Mode=none;Database=test;Ignore Prepare=false;AllowPublicKeyRetrieval=true"),
		new MySqlDbConnectorSettings
		{
			CancelUnfinishedCommands = cancelUnfinishedCommands,
		});

#if NET
	private const string c_framework = "netc";
#else
	private const string c_framework = "netf";
#endif
}
