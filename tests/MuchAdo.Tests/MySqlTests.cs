#if MYSQL
using System.Data;
using FluentAssertions;
using MuchAdo.SqlFormatting;
using MySqlConnector;
using NUnit.Framework;

namespace MuchAdo.Tests;

[TestFixture]
internal sealed class MySqlTests
{
	[Test]
	public void PrepareCacheTests()
	{
		var tableName = Sql.Name(nameof(PrepareCacheTests) + c_suffix);

		using var connector = CreateConnector();
		connector.Command(Sql.Format($"drop table if exists {tableName};")).Execute();
		connector.Command(Sql.Format($"create table {tableName} (Id int not null auto_increment primary key, Name varchar(100) not null);")).Execute();

		var insertSql = Sql.Format($"insert into {tableName} (Name) values (@itemA); insert into {tableName} (Name) values (@itemB);");
		connector.Command(insertSql).WithParameters(("itemA", "one"), ("itemB", "two")).Prepare().Cache().Execute().Should().Be(2);
		connector.Command(insertSql).WithParameters(("itemA", "three"), ("itemB", "four")).Prepare().Cache().Execute().Should().Be(2);
		connector.Command(insertSql).WithParameters(("itemB", "six"), ("itemA", "five")).Prepare().Cache().Execute().Should().Be(2);

		connector.Command(Sql.Format($"select Name from {tableName} order by Id;")).Query<string>().Should().Equal("one", "two", "three", "four", "five", "six");
	}

	[Test]
	public void SprocInOutTest()
	{
		var sprocName = nameof(SprocInOutTest) + c_suffix;

		using var connector = CreateConnector();
		connector.Command(Sql.Format($"drop procedure if exists {Sql.Name(sprocName)};")).Execute();
		connector.Command(Sql.Format($"create procedure {Sql.Name(sprocName)} (inout Value int) begin set Value = Value * Value; end;")).Execute();

		var param = new MySqlParameter { DbType = DbType.Int32, Direction = ParameterDirection.InputOutput, Value = 11 };
		connector.StoredProcedure(sprocName).WithParameter("Value", param).Execute();
		param.Value.Should().Be(121);
	}

	[Test]
	public void SprocInTest()
	{
		var sprocName = nameof(SprocInTest) + c_suffix;

		using var connector = CreateConnector();
		connector.Command(Sql.Format($"drop procedure if exists {Sql.Name(sprocName)};")).Execute();
		connector.Command(Sql.Format($"create procedure {Sql.Name(sprocName)} (in Value int) begin select Value, Value * Value; end;")).Execute();

		connector.StoredProcedure(sprocName).WithParameter("Value", 11).QuerySingle<(int, long)>().Should().Be((11, 121));
	}

	private static DbConnector CreateConnector() => new(
		new MySqlConnection("Server=localhost;User Id=root;Password=test;SSL Mode=none;Database=test;Ignore Prepare=false;AllowPublicKeyRetrieval=true"),
		new DbConnectorSettings { SqlSyntax = SqlSyntax.MySql });

#if NET9_0
	private const string c_suffix = "_net9";
#else
	private const string c_suffix = "_net472";
#endif
}
#endif
