using FluentAssertions;
using Microsoft.Data.Sqlite;
using NUnit.Framework;
using static FluentAssertions.FluentActions;

namespace MuchAdo.Sqlite.Tests;

[TestFixture]
internal sealed class SqliteTests
{
	[Test]
	public async Task ProviderAccessorsBatchTimeoutAndTransactions()
	{
		var tableName = Sql.Name(nameof(ProviderAccessorsBatchTimeoutAndTransactions));

		await using var connector = new SqliteDbConnector(
			new SqliteConnection("Data Source=:memory:"),
			new SqliteDbConnectorSettings { DefaultTimeout = TimeSpan.FromSeconds(9) });
		connector.Connection.Should().BeOfType<SqliteConnection>();
		(await connector.GetOpenConnectionAsync()).Should().BeSameAs(connector.Connection);
		connector.GetOpenConnection().Should().BeSameAs(connector.Connection);

		await connector
			.CommandFormat($"create table {tableName} (Id integer primary key, Name text not null)")
			.ExecuteAsync();

		await using (await connector.BeginTransactionAsync(SqliteDbTransactionSettings.Deferred))
		{
			connector.Transaction.Should().BeOfType<SqliteTransaction>();
			await connector.CommandFormat($"insert into {tableName} (Name) values ({"rollback"})").ExecuteAsync();
		}

		(await connector.CommandFormat($"select count(*) from {tableName}").QuerySingleAsync<long>()).Should().Be(0);

		await using (await connector.BeginTransactionAsync(SqliteDbTransactionSettings.Deferred))
		{
			connector.Transaction.Should().BeOfType<SqliteTransaction>();
			await connector.CommandFormat($"insert into {tableName} (Name) values ({"commit"})").ExecuteAsync();
			await connector.CommitTransactionAsync();
		}

		(await connector.CommandFormat($"select Name from {tableName}").QuerySingleAsync<string>()).Should().Be("commit");

		(await connector.CommandFormat($"select Name from {tableName}").WithTimeout(TimeSpan.FromSeconds(3)).QueryAsync(
			record =>
			{
				connector.ActiveCommand.Should().BeOfType<SqliteCommand>();
				connector.ActiveCommand!.CommandTimeout.Should().Be(3);
				connector.ActiveReader.Should().BeOfType<SqliteDataReader>();
				return record.Get<string>();
			})).Should().Equal("commit");

		var values = await connector
			.CommandFormat($"select count(*) from {tableName}")
			.CommandFormat($"select Name from {tableName}")
			.WithTimeout(TimeSpan.FromSeconds(4))
			.QueryMultipleAsync(
				async reader =>
				{
					connector.ActiveCommand.Should().BeNull();
					return (await reader.ReadSingleAsync<long>(), await reader.ReadSingleAsync<string>());
				});
		values.Should().Be((1, "commit"));
	}

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
	public void TypedAndRepeatTypedParametersApplyType()
	{
		var appliedSizes = new List<int>();
		var type = SqlParamType.Create(
			parameter =>
			{
				parameter.Size = 12;
				appliedSizes.Add(parameter.Size);
			});

		using var connector = new SqliteDbConnector(new SqliteConnection("Data Source=:memory:"));
		connector.Command("create table Items (Id integer primary key, Value text not null);").Execute();

		var repeated = Sql.RepeatParam("repeat", type);
		connector
			.CommandFormat($"insert into Items (Value) values ({Sql.Param("unnamed", type)})")
			.CommandFormat($"insert into Items (Value) values ({Sql.NamedParam("named", "named", type)})")
			.CommandFormat($"insert into Items (Value) values ({repeated}), ({repeated})")
			.Execute()
			.Should().Be(4);

		appliedSizes.Should().Equal(12, 12, 12);
		connector.Command("select Value from Items order by Id;").Query<string>().Should().Equal("unnamed", "named", "repeat", "repeat");
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
	public void BatchDataReader()
	{
		var tableName = Sql.Name(nameof(BatchDataReader));
		using var connector = new SqliteDbConnector(new SqliteConnection("Data Source=:memory:"));

		var affected = connector
			.CommandFormat($"drop table if exists {tableName}")
			.CommandFormat($"create table {tableName} (Id integer primary key, Name text not null)")
			.CommandFormat($"insert into {tableName} (Name) values ({"one"})")
			.CommandFormat($"insert into {tableName} (Name) values ({"two"})")
			.Execute();
		affected.Should().Be(2);

		var newId = connector
			.CommandFormat($"insert into {tableName} (Name) values ({"three"})")
			.Command("select last_insert_rowid()")
			.QuerySingle<long>();
		newId.Should().BeGreaterThan(0);

		var reader = connector
			.CommandFormat($"""
				select count(*) from {tableName} where Name like 'o%';
				insert into {tableName} (Name) values ({"four"});
				select count(*) from {tableName} where Name like 'z%';
				select count(*) from {tableName} where Name like 't%';
				""")
			.QueryMultiple();
		reader.ReadSingle<int>().Should().Be(1);
		reader.ReadSingle<int>().Should().Be(0);
		reader.ReadSingle<int>().Should().Be(2);
		Invoking(() => reader.Read<object>()).Should().Throw<InvalidOperationException>();

		// ensure behavior of batch matches behavior of delimited commands
		reader = connector
			.CommandFormat($"select count(*) from {tableName} where Name like 'o%'")
			.CommandFormat($"insert into {tableName} (Name) values ({"five"})")
			.CommandFormat($"select count(*) from {tableName} where Name like 'z%'")
			.CommandFormat($"select count(*) from {tableName} where Name like 't%'")
			.QueryMultiple();
		reader.ReadSingle<int>().Should().Be(1);
		reader.ReadSingle<int>().Should().Be(0);
		reader.ReadSingle<int>().Should().Be(2);
		Invoking(() => reader.Read<object>()).Should().Throw<InvalidOperationException>();
	}

	[Test]
	public void NullParameter()
	{
		using var connector = new SqliteDbConnector(new SqliteConnection("Data Source=:memory:"));
		connector.Command("create table Items (Id integer primary key, Value integer null);").Execute();

		int? value1 = null;
		connector.CommandFormat($"""
			insert into Items (Value)
			values ({value1})
			""").Execute();

		connector.Command("select count(*) from Items where Value is null;")
			.QuerySingle<long>()
			.Should().Be(1);
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
