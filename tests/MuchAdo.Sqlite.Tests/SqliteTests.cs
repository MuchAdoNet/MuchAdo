using FluentAssertions;
using Microsoft.Data.Sqlite;
using NUnit.Framework;
using static FluentAssertions.FluentActions;

namespace MuchAdo.Sqlite.Tests;

[TestFixture]
internal sealed class SqliteTests
{
	[Test]
	public void DeferredTransaction()
	{
		var connectionString = new SqliteConnectionStringBuilder { DataSource = nameof(DeferredTransaction), Mode = SqliteOpenMode.Memory, Cache = SqliteCacheMode.Shared }.ConnectionString;
		var connectorSettings = new SqliteDbConnectorSettings { DefaultTimeout = TimeSpan.FromSeconds(5) };
		using var connector1 = new SqliteDbConnector(new SqliteConnection(connectionString), connectorSettings);
		using var connector2 = new SqliteDbConnector(new SqliteConnection(connectionString), connectorSettings);
		connector1.Command("create table Items (ItemId integer primary key, Name text not null);").Execute();
		connector1.Command("insert into Items (Name) values ('xyzzy');").Execute();
		using var transaction1 = connector1.BeginTransaction(SqliteDbTransactionSettings.Deferred);
		using var transaction2 = connector2.BeginTransaction(SqliteDbTransactionSettings.Deferred);
		connector1.Command("select count(*) from Items;").QuerySingle<long>().Should().Be(1);
		connector2.Command("select count(*) from Items;").QuerySingle<long>().Should().Be(1);
		connector1.CommitTransaction();
	}

	[Test]
	public void PrepareCacheTests()
	{
		var tableName = Sql.Name(nameof(PrepareCacheTests));

		using var connector = CreateConnector();
		connector.CommandFormat($"drop table if exists {tableName};").Execute();
		connector.CommandFormat($"create table {tableName} (ItemId integer primary key, Name text not null);").Execute();

		var insertSql = Sql.Format($"insert into {tableName} (Name) values (@itemA); insert into {tableName} (Name) values (@itemB);");
		connector.Command(insertSql, Sql.NamedParam("itemA", "one"), Sql.NamedParam("itemB", "two")).Prepare().Cache().Execute().Should().Be(2);
		connector.Command(insertSql, Sql.NamedParam("itemA", "three"), Sql.NamedParam("itemB", "four")).Prepare().Cache().Execute().Should().Be(2);

		Invoking(() => connector.Command(insertSql, Sql.NamedParam("itemA", "five"), Sql.NamedParam("itemB", "six"), Sql.NamedParam("itemC", "seven")).Prepare().Cache().Execute()).Should().Throw<InvalidOperationException>();
		Invoking(() => connector.Command(insertSql, Sql.NamedParam("itemA", "five")).Prepare().Cache().Execute()).Should().Throw<InvalidOperationException>();
		Invoking(() => connector.Command(insertSql, Sql.NamedParam("itemB", "six"), Sql.NamedParam("itemA", "five")).Prepare().Cache().Execute()).Should().Throw<InvalidOperationException>();

		connector.Command(Sql.Format($"select Name from {tableName} order by ItemId;"))
			.Query<string>().Should().Equal("one", "two", "three", "four");
	}

	[Test]
	public void InsertAndSelectNameValue()
	{
		var tableName = Sql.Name(nameof(InsertAndSelectNameValue));

		using var connector = CreateConnector();
		connector.Command(Sql.Format($"drop table if exists {tableName};")).Execute();
		connector.Command(Sql.Format($"create table {tableName} (ItemId integer primary key, Name text not null, Value text not null);")).Execute();

		var items = new[] { new NameValue("one", "two"), new NameValue("two", "four") };

		connector.Command(Sql.Format($@"
				insert into {tableName} ({Sql.DtoColumnNames<NameValue>()})
				values {Sql.List(items.Select(item => Sql.Format($"({Sql.DtoParams(item)})")))};
				")).Execute();

		connector.Command(Sql.Format($"select {Sql.DtoColumnNames<NameValue>()} from {tableName} t order by ItemId;"))
			.Query<NameValue>().Should().Equal(items);
		connector.Command(Sql.Format($"select {Sql.DtoColumnNames<NameValue>().From(nameof(InsertAndSelectNameValue))} from {tableName} order by ItemId;"))
			.Query<NameValue>().Should().Equal(items);
		connector.Command(Sql.Format($"select {Sql.DtoColumnNames<NameValue>().From("t")} from {tableName} t order by ItemId;"))
			.Query<NameValue>().Should().Equal(items);
	}

	[Test]
	public void ChainedCommandFormatBatch()
	{
		var tableName = Sql.Name(nameof(ChainedCommandFormatBatch));
		using var connector = new SqliteDbConnector(new SqliteConnection("Data Source=:memory:"));

		var affected = connector
			.CommandFormat($"drop table if exists {tableName};")
			.CommandFormat($"create table {tableName} (Id integer primary key, Name text not null);")
			.CommandFormat($"insert into {tableName} (Name) values ({"one"});")
			.Execute();

		affected.Should().Be(1);

		var newId = connector
			.CommandFormat($"insert into {tableName} (Name) values ({"two"});")
			.Command("select last_insert_rowid();")
			.QuerySingle<long>();
		newId.Should().BeGreaterThan(0);
	}

	private static DbConnector CreateConnector() =>
		new(new SqliteConnection("Data Source=:memory:"), new DbConnectorSettings { SqlSyntax = SqlSyntax.Sqlite });

	private readonly struct NameValue
	{
		public NameValue(string name, string value) => (Name, Value) = (name, value);
		public string Name { get; init; }
		public string Value { get; init; }
	}
}
