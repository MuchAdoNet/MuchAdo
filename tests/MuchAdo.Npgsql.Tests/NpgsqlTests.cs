#if NPGSQL
using FluentAssertions;
using Npgsql;
using NUnit.Framework;

namespace MuchAdo.Npgsql.Tests;

[TestFixture]
internal sealed class NpgsqlTests
{
	[OneTimeSetUp]
	public void OneTimeSetUp()
	{
		AppContext.SetSwitch("Npgsql.EnableSqlRewriting", false);
	}

	[Test]
	public void PrepareCacheTests()
	{
		var tableName = Sql.Name($"{nameof(PrepareCacheTests)}_{c_framework}");

		using var connector = CreateConnector();
		connector.CommandFormat($"drop table if exists {tableName}").Execute();
		connector.CommandFormat($"create table {tableName} (ItemId serial primary key, Number integer not null)").Execute();

		var param = Sql.Param(1);
		var insertSql = Sql.Format($"insert into {tableName} (Number) values ({param});");
		connector.Command(insertSql).Prepare().Cache().Execute().Should().Be(1);
		param.Value = 2;
		connector.Command(insertSql).Prepare().Cache().Execute().Should().Be(1);

		connector.CommandFormat($"select Number from {tableName} order by ItemId;").Query<int>().Should().Equal(1, 2);
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
			.CommandFormat($"create table {tableName} (ItemId serial primary key, Name varchar not null)")
			.CommandFormat($"insert into {tableName} (Name) values ($1), ($2)", Sql.Param("one"), Sql.Param("two"))
			.Execute();

		var three = Sql.ReusedParam("three");
		var four = "four";
		connector.CommandFormat($"insert into {tableName} (Name) values ({three}), ({four}), ({three}), ({four})").Execute();
		lastCommandText.Should().Contain("(Name) values ($1), ($2), ($1), ($3)");

		connector.CommandFormat($"select Name from {tableName} order by ItemId").Query<string>().Should().Equal("one", "two", "three", "four", "three", "four");
	}

	private static NpgsqlDbConnector CreateConnector(bool cancelUnfinishedCommands = false) => new(
		new NpgsqlConnection("host=localhost;user id=root;password=test;database=test"),
		new NpgsqlDbConnectorSettings
		{
			CancelUnfinishedCommands = cancelUnfinishedCommands,
		});

#if NET
	private const string c_framework = "netc";
#else
	private const string c_framework = "netf";
#endif
}
#endif
