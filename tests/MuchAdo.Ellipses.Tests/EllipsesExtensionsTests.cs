using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using NUnit.Framework;
using static FluentAssertions.FluentActions;

namespace MuchAdo.Ellipses.Tests;

[TestFixture]
[SuppressMessage("ReSharper", "InterpolatedStringExpressionIsNotIFormattable", Justification = "Custom formatting.")]
internal sealed class EllipsesExtensionsTests
{
	[Test]
	public void ParameterCollectionTests()
	{
		using var connector = CreateConnector();
		var tableName = Sql.Raw(nameof(ParameterCollectionTests));
		connector.CommandFormat($"create table {tableName} (ItemId integer primary key, Name text not null);").Execute().Should().Be(0);
		connector.CommandFormat($"insert into {tableName} (Name) values ('one'), ('two'), ('three');").Execute().Should().Be(3);
		var reader = connector
			.CommandFormat($"""
				select Name from {tableName} where Name in (@names...);
				select Name from {tableName} where Name not in (@names...);
				select @before + @after;
				""",
				Sql.NamedParam("before", 1),
				Sql.NamedParam("names", new[] { "one", "three", "five" }),
				Sql.NamedParam("ignore", new[] { 0 }),
				Sql.NamedParam("after", 2))
			.QueryMultiple();
		reader.Read<string>().Should().BeEquivalentTo("one", "three");
		reader.Read<string>().Should().BeEquivalentTo("two");
		reader.Read<long>().Should().BeEquivalentTo([3L]);
	}

	[Test]
	public void BadParameterCollectionTests()
	{
		using var connector = CreateConnector();
		connector.Command("create table Items (ItemId integer primary key, Name text not null);").Execute().Should().Be(0);
		connector.Command("insert into Items (Name) values ('one'), ('two'), ('three');").Execute().Should().Be(3);
		Invoking(() => connector.Command("select Name from Items where Name in (@names...);", Sql.NamedParam("names", Array.Empty<string>()))
			.Query<string>()).Should().Throw<InvalidOperationException>();
	}

	private static DbConnector CreateConnector()
	{
		var connector = new DbConnector(new SqliteConnection("Data Source=:memory:"));
		connector.Executing += (_, e) => e.CommandBatch.ExpandEllipses();
		return connector;
	}
}
