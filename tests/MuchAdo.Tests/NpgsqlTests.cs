#if NPGSQL
using FluentAssertions;
using MuchAdo.SqlFormatting;
using Npgsql;
using NUnit.Framework;

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
		connector.Command(insertSql).WithParameters(("itemA", "one"), ("itemB", "two")).Prepare().Cache().Execute().Should().Be(2);
		connector.Command(insertSql).WithParameters(("itemA", "three"), ("itemB", "four")).Prepare().Cache().Execute().Should().Be(2);
		connector.Command(insertSql).WithParameters(("itemB", "six"), ("itemA", "five")).Prepare().Cache().Execute().Should().Be(2);

		// fails if parameters aren't reused properly
		connector.Command(Sql.Format($"select Name from {tableName} order by ItemId;"))
			.Query<string>().Should().Equal("one", "two", "three", "four", "five", "six");
	}

	private static DbConnector CreateConnector() => new(
		new NpgsqlConnection("host=localhost;user id=root;password=test;database=test"),
		new DbConnectorSettings { SqlSyntax = SqlSyntax.Postgres });

#if NET9_0
	private const string c_suffix = "_net9";
#else
	private const string c_suffix = "_net472";
#endif
}
#endif
