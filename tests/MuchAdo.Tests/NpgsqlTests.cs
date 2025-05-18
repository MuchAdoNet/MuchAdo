#if NPGSQL
using FluentAssertions;
using Npgsql;
using NUnit.Framework;
using static FluentAssertions.FluentActions;

namespace MuchAdo.Tests;

[TestFixture]
internal sealed class NpgsqlTests
{
	[Test]
	public void PrepareCacheTests()
	{
		var tableName = Sql.Name(nameof(PrepareCacheTests) + c_suffix);

		using var connector = CreateConnector();
		connector.Command(Sql.Format($"drop table if exists {tableName};")).Execute();
		connector.Command(Sql.Format($"create table {tableName} (ItemId serial primary key, Name varchar not null);")).Execute();

		var insertSql = Sql.Format($"insert into {tableName} (Name) values (@itemA); insert into {tableName} (Name) values (@itemB);");
		connector.Command(insertSql, Sql.NamedParam("itemA", "one"), Sql.NamedParam("itemB", "two")).Prepare().Cache().Execute().Should().Be(2);
		connector.Command(insertSql, Sql.NamedParam("itemA", "three"), Sql.NamedParam("itemB", "four")).Prepare().Cache().Execute().Should().Be(2);

		Invoking(() => connector.Command(insertSql, Sql.NamedParam("itemA", "five"), Sql.NamedParam("itemB", "six"), Sql.NamedParam("itemC", "seven")).Prepare().Cache().Execute()).Should().Throw<InvalidOperationException>();
		Invoking(() => connector.Command(insertSql, Sql.NamedParam("itemA", "five")).Prepare().Cache().Execute()).Should().Throw<InvalidOperationException>();
		Invoking(() => connector.Command(insertSql, Sql.NamedParam("itemB", "six"), Sql.NamedParam("itemA", "five")).Prepare().Cache().Execute()).Should().Throw<InvalidOperationException>();

		connector.Command(Sql.Format($"select Name from {tableName} order by ItemId;")).Query<string>().Should().Equal("one", "two", "three", "four");
	}

	[Test]
	public void UnnamedParameterTest()
	{
		var tableName = Sql.Name(nameof(UnnamedParameterTest) + c_suffix);

		using var connector = CreateConnector();

		var lastCommandText = "";
		connector.Executing += (_, e) => lastCommandText = e.CommandBatch.LastCommand.BuildText(connector.SqlSyntax);

		connector.Command(Sql.Format($"drop table if exists {tableName};")).Execute();
		connector.Command(Sql.Format($"create table {tableName} (ItemId serial primary key, Name varchar not null);")).Execute();
		connector.Command(Sql.Format($"insert into {tableName} (Name) values ($1), ($2);"), Sql.Param("one"), Sql.Param("two")).Execute();

		var three = Sql.ReusedParam("three");
		var four = "four";
		connector.CommandFormat($"insert into {tableName} (Name) values ({three}), ({four}), ({three}), ({four});").Execute();
		lastCommandText.Should().Contain("(Name) values ($1), ($2), ($1), ($3);");

		connector.Command(Sql.Format($"select Name from {tableName} order by ItemId;")).Query<string>().Should().Equal("one", "two", "three", "four", "three", "four");
	}

	private static DbConnector CreateConnector() => new(
		new NpgsqlConnection("host=localhost;user id=root;password=test;database=test"),
		new DbConnectorSettings { SqlSyntax = SqlSyntax.Postgres.WithUnnamedParameterStrategy(SqlUnnamedParameterStrategy.Numbered("$")) });

#if NET
	private const string c_suffix = "_netc";
#else
	private const string c_suffix = "_netf";
#endif
}
#endif
