using System.Data;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using NUnit.Framework;
using static FluentAssertions.FluentActions;

namespace MuchAdo.SqlServer.Tests;

[TestFixture, Category("Docker"), NonParallelizable]
internal sealed class SqlServerTests
{
	[Test]
	public async Task ProviderAccessorsTimeoutTransactionsAndSprocs()
	{
		var tableName = Sql.Name($"{nameof(ProviderAccessorsTimeoutTransactionsAndSprocs)}{c_suffix}");
		var inSprocName = $"{nameof(ProviderAccessorsTimeoutTransactionsAndSprocs)}In{c_suffix}";
		var inOutSprocName = $"{nameof(ProviderAccessorsTimeoutTransactionsAndSprocs)}InOut{c_suffix}";

		await using var connector = CreateConnector();
		connector.Connection.Should().BeOfType<SqlConnection>();
		(await connector.GetOpenConnectionAsync()).Should().BeSameAs(connector.Connection);
		connector.GetOpenConnection().Should().BeSameAs(connector.Connection);

		await connector.Command(Sql.Format($"drop table if exists {tableName};")).ExecuteAsync();
		await connector.Command(Sql.Format($"create table {tableName} (Id int not null identity primary key, Name nvarchar(100) not null); ")).ExecuteAsync();
		await connector.CommandFormat($"create or alter procedure {Sql.Name(inSprocName)} @Value int as select @Value, @Value * @Value;").ExecuteAsync();
		await connector.CommandFormat($"create or alter procedure {Sql.Name(inOutSprocName)} @Value int output as set @Value = @Value * @Value;").ExecuteAsync();

		await using (await connector.BeginTransactionAsync())
		{
			connector.Transaction.Should().BeOfType<SqlTransaction>();
			await connector.CommandFormat($"insert into {tableName} (Name) values ({"rollback"})").ExecuteAsync();
		}

		(await connector.CommandFormat($"select count(*) from {tableName}").QuerySingleAsync<int>()).Should().Be(0);

		await using (await connector.BeginTransactionAsync())
		{
			connector.Transaction.Should().BeOfType<SqlTransaction>();
			await connector.CommandFormat($"insert into {tableName} (Name) values ({"commit"})").ExecuteAsync();
			await connector.CommitTransactionAsync();
		}

		(await connector.CommandFormat($"select Name from {tableName}").QuerySingleAsync<string>()).Should().Be("commit");

		(await connector.CommandFormat($"select Name from {tableName}").WithTimeout(TimeSpan.FromSeconds(3)).QueryAsync(
			record =>
			{
				connector.ActiveCommand.Should().BeOfType<SqlCommand>();
				connector.ActiveCommand!.CommandTimeout.Should().Be(3);
				connector.ActiveReader.Should().BeOfType<SqlDataReader>();
				return record.Get<string>();
			})).Should().Equal("commit");

		(await connector.StoredProcedure(inSprocName, Sql.NamedParam("Value", 11)).QuerySingleAsync<(int, int)>()).Should().Be((11, 121));

		var param = new SqlParameter("Value", SqlDbType.Int) { Direction = ParameterDirection.InputOutput, Value = 11 };
		await connector.StoredProcedure(inOutSprocName, param).ExecuteAsync();
		param.Value.Should().Be(121);
	}

	[Test]
	public async Task PrepareCacheTests()
	{
		var tableName = Sql.Name(nameof(PrepareCacheTests) + c_suffix);

		await using var connector = CreateConnector();
		await connector.Command(Sql.Format($"drop table if exists {tableName};")).ExecuteAsync();
		await connector.Command(Sql.Format($"create table {tableName} (ItemId int not null identity primary key, Name nvarchar(100) not null);")).ExecuteAsync();

		var insertSql = Sql.Format($"insert into {tableName} (Name) values (@itemA); insert into {tableName} (Name) values (@itemB);");
		(await connector.Command(insertSql, Sql.NamedParam("itemA", CreateStringParameter("one")), Sql.NamedParam("itemB", CreateStringParameter("two"))).Prepare().Cache().ExecuteAsync()).Should().Be(2);
		(await connector.Command(insertSql, Sql.NamedParam("itemA", CreateStringParameter("three")), Sql.NamedParam("itemB", CreateStringParameter("four"))).Prepare().Cache().ExecuteAsync()).Should().Be(2);

		await Invoking(async () => await connector.Command(insertSql, Sql.NamedParam("itemA", "five"), Sql.NamedParam("itemB", "six"), Sql.NamedParam("itemC", "seven")).Prepare().Cache().ExecuteAsync()).Should().ThrowAsync<InvalidOperationException>();
		await Invoking(async () => await connector.Command(insertSql, Sql.NamedParam("itemA", "five")).Prepare().Cache().ExecuteAsync()).Should().ThrowAsync<InvalidOperationException>();
		await Invoking(async () => await connector.Command(insertSql, Sql.NamedParam("itemB", "six"), Sql.NamedParam("itemA", "five")).Prepare().Cache().ExecuteAsync()).Should().ThrowAsync<InvalidOperationException>();

		// SqlCommand.Prepare method requires all parameters to have an explicitly set type
		SqlParameter CreateStringParameter(string value) => new SqlParameter { Value = value, DbType = DbType.String, Size = 100 };
	}

	[Test]
	public async Task NullParameter()
	{
		var tableName = Sql.Name($"{nameof(NullParameter)}{c_suffix}");

		await using var connector = CreateConnector();
		await connector.Command(Sql.Format($"drop table if exists {tableName};")).ExecuteAsync();
		await connector.Command(Sql.Format($"create table {tableName} (Id int not null identity primary key, Value int null);")).ExecuteAsync();

		int? value1 = null;
		await connector.CommandFormat($"""
			insert into {tableName} (Value)
			values ({value1})
			""").ExecuteAsync();

		(await connector.CommandFormat($"select count(*) from {tableName} where Value is null")
			.QuerySingleAsync<int>())
			.Should().Be(1);
	}

	private static SqlServerDbConnector CreateConnector() => new(
		new SqlConnection(GetConnectionString()));

	private static string GetConnectionString() =>
		Environment.GetEnvironmentVariable("MUCHADO_SQLSERVER_TEST_CONNECTION_STRING") ??
		"data source=localhost;user id=sa;password=P@ssw0rd;initial catalog=test;TrustServerCertificate=True";

#if NET
	private const string c_suffix = "_netc";
#else
	private const string c_suffix = "_netf";
#endif
}
