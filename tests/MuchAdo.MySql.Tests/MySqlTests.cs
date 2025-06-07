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
	public void PrepareCacheTests()
	{
		var tableName = Sql.Name($"{nameof(PrepareCacheTests)}_{c_framework}");

		using var connector = CreateConnector();
		connector
			.CommandFormat($"drop table if exists {tableName}")
			.CommandFormat($"create table {tableName} (Id int not null auto_increment primary key, Name varchar(100) not null)")
			.Execute();

		var insertSql = Sql.Format($"insert into {tableName} (Name) values (@itemA); insert into {tableName} (Name) values (@itemB);");
		connector.Command(insertSql, Sql.NamedParams(("itemA", "one"), ("itemB", "two"))).Prepare().Cache().Execute().Should().Be(2);
		connector.Command(insertSql, Sql.NamedParams(("itemA", "three"), ("itemB", "four"))).Prepare().Cache().Execute().Should().Be(2);

		Invoking(() => connector.Command(insertSql, Sql.NamedParams(("itemA", "five"), ("itemB", "six"), ("itemC", "seven"))).Prepare().Cache().Execute()).Should().Throw<InvalidOperationException>();
		Invoking(() => connector.Command(insertSql, Sql.NamedParam("itemA", "five")).Prepare().Cache().Execute()).Should().Throw<InvalidOperationException>();
		Invoking(() => connector.Command(insertSql, Sql.NamedParams(("itemB", "six"), ("itemA", "five"))).Prepare().Cache().Execute()).Should().Throw<InvalidOperationException>();

		connector.CommandFormat($"select Name from {tableName} order by Id").Query<string>().Should().Equal("one", "two", "three", "four");
	}

	[TestCase(false)]
	[TestCase(true)]
	public void CancelTest(bool autoCancel)
	{
		var tableName = Sql.Name($"{nameof(CancelTest)}_{autoCancel}_{c_framework}");

		using var connector = CreateConnector(cancelUnfinishedCommands: autoCancel);

		var stopwatch = Stopwatch.StartNew();
		foreach (var value in connector
			.CommandFormat($"drop table if exists {tableName}")
			.CommandFormat($"create table {tableName} (Id int not null auto_increment primary key, Value int not null)")
			.CommandFormat($"insert into {tableName} (Value) values {Sql.List(Enumerable.Range(1, 100).Select(x => Sql.Format($"({x})")))}")
			.CommandFormat($"select t1.Value from {tableName} t1 join {tableName} t2 join {tableName} t3 join {tableName} t4")
			.Enumerate<int>())
		{
			if (!autoCancel)
				connector.Cancel();
			break;
		}

		// without cancel, disposing the reader still has to wait for the many rows to be downloaded
		stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(5));
	}

	[Test]
	public void SprocInOutTest()
	{
		var sprocName = $"{nameof(SprocInOutTest)}_{c_framework}";

		using var connector = CreateConnector();
		connector
			.CommandFormat($"drop procedure if exists {Sql.Name(sprocName)}")
			.CommandFormat($"create procedure {Sql.Name(sprocName)} (inout Value int) begin set Value = Value * Value; end")
			.Execute();

		var param = new MySqlParameter("Value", MySqlDbType.Int32) { Direction = ParameterDirection.InputOutput, Value = 11 };
		connector.StoredProcedure(sprocName, param).Execute();
		param.Value.Should().Be(121);
	}

	[Test]
	public void SprocInTest()
	{
		var sprocName = $"{nameof(SprocInTest)}_{c_framework}";

		using var connector = CreateConnector();
		connector
			.CommandFormat($"drop procedure if exists {Sql.Name(sprocName)}")
			.CommandFormat($"create procedure {Sql.Name(sprocName)} (in Value int) begin select Value, Value * Value; end")
			.Execute();

		connector.StoredProcedure(sprocName, Sql.NamedParam("Value", 11)).QuerySingle<(int, long)>().Should().Be((11, 121));
	}

	[Test]
	public void UnnamedParameterTest()
	{
		var tableName = Sql.Name($"{nameof(UnnamedParameterTest)}_{c_framework}");

		using var connector = CreateConnector();

		var lastCommandText = "";
		connector.Executing += (_, e) => lastCommandText = e.CommandBatch.LastCommand.BuildText(connector.SqlSyntax);

		connector
			.CommandFormat($"drop table if exists {tableName}")
			.CommandFormat($"create table {tableName} (Id int not null auto_increment primary key, Name varchar(100) not null)")
			.CommandFormat($"insert into {tableName} (Name) values (?), (?)", Sql.Param("one"), Sql.Param("two"))
			.Execute();

		var three = Sql.Param("three");
		var four = "four";
		connector.CommandFormat($"insert into {tableName} (Name) values ({three}), ({four}), ({three}), ({four})").Execute();
		lastCommandText.Should().Contain("(Name) values (?), (?), (?), (?)");

		connector.CommandFormat($"select Name from {tableName} order by Id").Query<string>().Should().Equal("one", "two", "three", "four", "three", "four");
	}

	[Test]
	public void MySqlDecimalTest()
	{
		var tableName = Sql.Name($"{nameof(MySqlDecimalTest)}_{c_framework}");

		using var connector = CreateConnector();

		connector
			.CommandFormat($"drop table if exists {tableName}")
			.CommandFormat($"create table {tableName} (Id int not null auto_increment primary key, Value decimal(10, 2) not null)")
			.CommandFormat($"insert into {tableName} (Value) values (?)", Sql.Param(6.875m))
			.Execute();

		connector.Command(Sql.Format($"select Value from {tableName}")).QuerySingle<decimal>().Should().Be(6.88m);
		connector.Command(Sql.Format($"select Value from {tableName}")).QuerySingle<MySqlDecimal>().Value.Should().Be(6.88m);
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
