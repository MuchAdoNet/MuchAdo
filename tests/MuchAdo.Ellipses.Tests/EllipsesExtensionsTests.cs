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
		connector.Command("create table Items (ItemId integer primary key, Name text not null);").Execute().Should().Be(0);
		connector.Command("insert into Items (Name) values ('one'), ('two'), ('three');").Execute().Should().Be(3);
		var resultSets = connector
			.Command("""
				select Name from Items where Name in (@names...);
				select Name from Items where Name not in (@names...);
				select @before + @after;
				""")
			.WithParameter("before", 1)
			.WithParameter("names", new[] { "one", "three", "five" })
			.WithParameter("ignore", new[] { 0 })
			.WithParameter("after", 2)
			.ExpandEllipses()
			.QueryMultiple();
		resultSets.Read<string>().Should().BeEquivalentTo("one", "three");
		resultSets.Read<string>().Should().BeEquivalentTo("two");
		resultSets.Read<long>().Should().BeEquivalentTo([3L]);
	}

	[Test]
	public void BadParameterCollectionTests()
	{
		using var connector = CreateConnector();
		connector.Command("create table Items (ItemId integer primary key, Name text not null);").Execute().Should().Be(0);
		connector.Command("insert into Items (Name) values ('one'), ('two'), ('three');").Execute().Should().Be(3);
		Invoking(() => connector.Command("select Name from Items where Name in (@names...);").WithParameter("names", Array.Empty<string>()).ExpandEllipses()
			.Query<string>()).Should().Throw<InvalidOperationException>();
	}

	private static DbConnector CreateConnector(DefaultDbTypeMapperSettings? defaultTypeMapperSettings = null) =>
		new(new SqliteConnection("Data Source=:memory:"),
			defaultTypeMapperSettings is null ? null : new DbConnectorSettings { DataMapper = new DbDataMapper(new DefaultDbTypeMapperFactory(defaultTypeMapperSettings)) });
}
