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
	public void BatchDataReaderDelegatesMetadataAndTypedAccessors()
	{
		using var connector = new SqliteDbConnector(new SqliteConnection("Data Source=:memory:"));

		var values = connector
			.Command("select cast(42 as integer) as Id, 'one' as Name, cast(2.5 as real) as Amount")
			.Command("select 'second' as Name")
			.QueryMultiple(
				_ =>
				{
					var reader = ((DbConnector) connector).ActiveReader!;
					reader.FieldCount.Should().Be(3);
					reader.GetName(0).Should().Be("Id");
					reader.GetOrdinal("Name").Should().Be(1);
					reader.GetFieldType(0).Should().Be<long>();
					reader.GetDataTypeName(2).Should().NotBeNullOrEmpty();

					reader.Read().Should().BeTrue();
					reader.GetInt64(0).Should().Be(42);
					reader.GetString(1).Should().Be("one");
					reader.GetDouble(2).Should().Be(2.5);
					reader[0].Should().Be(42L);
					reader["Name"].Should().Be("one");
					var fieldValues = new object[3];
					reader.GetValues(fieldValues).Should().Be(3);
					fieldValues.Should().Equal(42L, "one", 2.5);

					reader.NextResult().Should().BeTrue();
					reader.Read().Should().BeTrue();
					return (reader.FieldCount, reader.GetString(0));
				});

		values.Should().Be((1, "second"));
	}

	[Test]
	public void BatchDataReaderSkipsNonRowCommandsBetweenResultSets()
	{
		using var connector = new SqliteDbConnector(new SqliteConnection("Data Source=:memory:"));

		var values = connector
			.Command("create table Items (Id integer primary key, Name text not null)")
			.Command("insert into Items (Name) values ('one')")
			.Command("select count(*) from Items")
			.Command("update Items set Name = 'two' where Id = 1")
			.Command("select Name from Items where Id = 1")
			.QueryMultiple(reader => (reader.ReadSingle<long>(), reader.ReadSingle<string>()));

		values.Should().Be((1, "two"));
	}

	[Test]
	public void BatchDataReaderDisposedReaderThrowsForReadAndNextResult()
	{
		using var connector = new SqliteDbConnector(new SqliteConnection("Data Source=:memory:"));
		using var resultSetReader = connector
			.Command("select 1")
			.Command("select 2")
			.QueryMultiple();
		var reader = ((DbConnector) connector).ActiveReader!;

		resultSetReader.Dispose();

		Invoking(reader.Read).Should().Throw<ObjectDisposedException>();
		Invoking(reader.NextResult).Should().Throw<ObjectDisposedException>();
	}

	[Test]
	public void BatchDataReaderEmptyBatchThrows()
	{
		using var connector = new SqliteDbConnector(new SqliteConnection("Data Source=:memory:"));

		Invoking(() => connector.CreateCommandBatch().QueryMultiple())
			.Should().Throw<InvalidOperationException>()
			.WithMessage("The command batch is empty.");
	}

	[Test]
	public async Task BatchDataReaderAsyncPreparedBatch()
	{
		await using var connector = new SqliteDbConnector(new SqliteConnection("Data Source=:memory:"));

		await connector.Command("create table Items (Id integer primary key, Name text not null)").ExecuteAsync();

		(await connector
			.Command("insert into Items (Name) values ('one')")
			.Prepare()
			.ExecuteAsync()).Should().Be(1);

		var values = await connector
			.Command("select count(*) from Items")
			.Command("insert into Items (Name) values ('two')")
			.Command("select Name from Items order by Id")
			.Prepare()
			.QueryMultipleAsync(
				async reader =>
				{
					var count = await reader.ReadSingleAsync<long>();
					var names = await reader.ReadAsync<string>();
					return (count, names);
				});

		values.count.Should().Be(1);
		values.names.Should().Equal("one", "two");
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
