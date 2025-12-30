using FluentAssertions;
using Npgsql;
using NUnit.Framework;

namespace MuchAdo.Npgsql.Tests;

[TestFixture(Explicit = true)]
internal sealed class NpgsqlTests
{
	[OneTimeSetUp]
	public void OneTimeSetUp()
	{
		AppContext.SetSwitch("Npgsql.EnableSqlRewriting", false);
	}

	[Test]
	public async Task ReuseParameter()
	{
		var tableName = Sql.Name($"{nameof(ReuseParameter)}_{c_framework}");

		await using var connector = CreateConnector();
		await connector
			.CommandFormat($"drop table if exists {tableName}")
			.CommandFormat($"create table {tableName} (ItemId serial primary key, Number integer not null)")
			.ExecuteAsync();

		var param = Sql.Param(1);
		var insertSql = Sql.Format($"insert into {tableName} (Number) values ({param});");
		(await connector.Command(insertSql).ExecuteAsync()).Should().Be(1);
		param.Value = 2;
		(await connector.Command(insertSql).ExecuteAsync()).Should().Be(1);

		var values = await connector
			.CommandFormat($"select Number from {tableName} order by ItemId;")
			.QueryAsync<int>();
		values.Should().Equal(1, 2);
	}

	[Test]
	public async Task NullableValueTypeParameter([Values] bool prepare, [Values] bool cache)
	{
		var tableName = Sql.Name($"{nameof(ReuseParameter)}_{c_framework}");

		await using var connector = CreateConnector();
		await connector
			.CommandFormat($"drop table if exists {tableName}")
			.CommandFormat($"create table {tableName} (ItemId serial primary key, Number integer)")
			.ExecuteAsync();

		int? value = 1;
		(await connector.CommandFormat($"insert into {tableName} (Number) values ({value});").Prepare(prepare).Cache(cache).ExecuteAsync()).Should().Be(1);
		value = null;
		(await connector.CommandFormat($"insert into {tableName} (Number) values ({value});").Prepare(prepare).Cache(cache).ExecuteAsync()).Should().Be(1);
		value = 2;
		(await connector.CommandFormat($"insert into {tableName} (Number) values ({value});").Prepare(prepare).Cache(cache).ExecuteAsync()).Should().Be(1);

		var values = await connector
			.CommandFormat($"select Number from {tableName} order by ItemId;")
			.QueryAsync<int?>();
		values.Should().Equal(1, null, 2);
	}

	[Test]
	public async Task NullParameter()
	{
		var tableName = Sql.Name($"{nameof(NullParameter)}_{c_framework}");

		await using var connector = CreateConnector();
		await connector
			.CommandFormat($"drop table if exists {tableName}")
			.CommandFormat($"create table {tableName} (ItemId serial primary key, Value integer null)")
			.ExecuteAsync();

		int? value1 = null;
		await connector.CommandFormat($"""
			insert into {tableName} (Value)
			values ({value1})
			""").ExecuteAsync();

		(await connector.CommandFormat($"select count(*) from {tableName} where Value is null")
			.QuerySingleAsync<long>())
			.Should().Be(1);
	}

	[Test]
	public async Task RepeatParameter()
	{
		var tableName = Sql.Name($"{nameof(RepeatParameter)}_{c_framework}");

		await using var connector = CreateConnector();

		var lastCommandText = "";
		connector.Executing += (_, e) => lastCommandText = e.CommandBatch.GetCommand(e.CommandBatch.CommandCount - 1).BuildText(connector.SqlSyntax);

		await connector
			.CommandFormat($"drop table if exists {tableName}")
			.CommandFormat($"create table {tableName} (ItemId serial primary key, Name varchar not null)")
			.CommandFormat($"insert into {tableName} (Name) values ($1), ($2)", Sql.Param("one"), Sql.Param("two"))
			.ExecuteAsync();

		var three = Sql.RepeatParam("three");
		var four = "four";
		await connector
			.CommandFormat($"insert into {tableName} (Name) values ({three}), ({four}), ({three}), ({four})")
			.ExecuteAsync();
		lastCommandText.Should().Contain("(Name) values ($1), ($2), ($1), ($3)");

		var names = await connector
			.CommandFormat($"select Name from {tableName} order by ItemId")
			.QueryAsync<string>();
		names.Should().Equal("one", "two", "three", "four", "three", "four");
	}

	private static NpgsqlDbConnector CreateConnector() => new(
		new NpgsqlConnection("host=localhost;user id=root;password=test;database=test"));

#if NET
	private const string c_framework = "netc";
#else
	private const string c_framework = "netf";
#endif
}
