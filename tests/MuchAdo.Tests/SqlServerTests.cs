#if SQLSERVER
using System.Data;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using MuchAdo.SqlFormatting;
using NUnit.Framework;

namespace MuchAdo.Tests;

[TestFixture]
internal sealed class SqlServerTests
{
	[Test]
	public void PrepareCacheTests()
	{
		var tableName = Sql.Name(nameof(PrepareCacheTests) + c_suffix);

		using var connector = CreateConnector();
		connector.Command(Sql.Format($"drop table if exists {tableName};")).Execute();
		connector.Command(Sql.Format($"create table {tableName} (ItemId int not null identity primary key, Name nvarchar(100) not null);")).Execute();

		var insertSql = Sql.Format($"insert into {tableName} (Name) values (@itemA); insert into {tableName} (Name) values (@itemB);");
		connector.Command(insertSql).WithParameters(("itemA", CreateStringParameter("one")), ("itemB", CreateStringParameter("two"))).Prepare().Cache().Execute().Should().Be(2);
		connector.Command(insertSql).WithParameters(("itemA", CreateStringParameter("three")), ("itemB", CreateStringParameter("four"))).Prepare().Cache().Execute().Should().Be(2);
		connector.Command(insertSql).WithParameters(("itemB", CreateStringParameter("six")), ("itemA", CreateStringParameter("five"))).Prepare().Cache().Execute().Should().Be(2);

		// fails if parameters aren't reused properly
		connector.Command(Sql.Format($"select Name from {tableName} order by ItemId;"))
			.Query<string>().Should().Equal("one", "two", "three", "four", "five", "six");

		// SqlCommand.Prepare method requires all parameters to have an explicitly set type
		SqlParameter CreateStringParameter(string value) => new SqlParameter { Value = value, DbType = DbType.String, Size = 100 };
	}

	private static DbConnector CreateConnector() => new(
		new SqlConnection("data source=localhost;user id=sa;password=P@ssw0rd;initial catalog=test;TrustServerCertificate=True"),
		new DbConnectorSettings { SqlSyntax = SqlSyntax.SqlServer });

#if NET9_0
	private const string c_suffix = "_net9";
#else
	private const string c_suffix = "_net472";
#endif
}
#endif
