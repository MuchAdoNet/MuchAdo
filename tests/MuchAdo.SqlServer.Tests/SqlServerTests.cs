using System.Data;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using NUnit.Framework;
using static FluentAssertions.FluentActions;

namespace MuchAdo.SqlServer.Tests;

[TestFixture(Explicit = true)]
internal sealed class SqlServerTests
{
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

	private static DbConnector CreateConnector() => new(
		new SqlConnection("data source=localhost;user id=sa;password=P@ssw0rd;initial catalog=test;TrustServerCertificate=True"),
		new DbConnectorSettings { SqlSyntax = SqlSyntax.SqlServer });

#if NET
	private const string c_suffix = "_netc";
#else
	private const string c_suffix = "_netf";
#endif
}
