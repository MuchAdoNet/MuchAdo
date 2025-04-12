using FluentAssertions;
using Microsoft.Data.Sqlite;
using NUnit.Framework;
using static FluentAssertions.FluentActions;

namespace MuchAdo.Ellipses.Tests;

[TestFixture]
internal sealed class BulkInsertUtilityTests
{
	[Test]
	public void BulkInsertTests()
	{
		using var connector = CreateConnector();
		connector.Command("create table Items (ItemId integer primary key, Name text not null);").Execute();
		connector.Command("insert into Items (Name) values (@name)...;")
			.BulkInsert(Enumerable.Range(1, 100).Select(x => DbParameters.Create("name", $"item{x}")));
		connector.Command("select count(*) from Items;").QuerySingle<long>().Should().Be(100);
	}

	[Test]
	public async Task BulkInsertAsyncTests()
	{
		await using var connector = CreateConnector();
		await connector.Command("create table Items (ItemId integer primary key, Name text not null);").ExecuteAsync();
		await connector.Command("insert into Items (Name) values (@name)...;")
			.BulkInsertAsync(Enumerable.Range(1, 100).Select(x => DbParameters.Create("name", $"item{x}")));
		(await connector.Command("select count(*) from Items;").QuerySingleAsync<long>()).Should().Be(100);
	}

	[Test]
	public void EmptySql_Throws()
	{
		Invoking(() => BulkInsertUtility.GetBulkInsertCommands("", DbParameters.Empty, [DbParameters.FromDto(new { foo = 1 })]).ToList())
			.Should().Throw<ArgumentException>();
	}

	[Test]
	public void NoValues_Throws()
	{
		Invoking(() => BulkInsertUtility.GetBulkInsertCommands("VALUE (@foo)...", DbParameters.Empty, [DbParameters.FromDto(new { foo = 1 })]).ToList())
			.Should().Throw<ArgumentException>();
	}

	[Test]
	public void ValuesSuffix_Throws()
	{
		Invoking(() => BulkInsertUtility.GetBulkInsertCommands("1VALUES (@foo)...", DbParameters.Empty, [DbParameters.FromDto(new { foo = 1 })]).ToList())
			.Should().Throw<ArgumentException>();
	}

	[Test]
	public void NoEllipsis_Throws()
	{
		Invoking(() => BulkInsertUtility.GetBulkInsertCommands("VALUE (@foo)..", DbParameters.Empty, [DbParameters.FromDto(new { foo = 1 })]).ToList())
			.Should().Throw<ArgumentException>();
	}

	[Test]
	public void MultipleValues_Throws()
	{
		Invoking(() => BulkInsertUtility.GetBulkInsertCommands("VALUES (@foo)... VALUES (@foo)...", DbParameters.Empty, [DbParameters.FromDto(new { foo = 1 })]).ToList())
			.Should().Throw<ArgumentException>();
	}

	[Test]
	public void ZeroBatchSize_Throws()
	{
		Invoking(() => BulkInsertUtility.GetBulkInsertCommands("VALUES (@foo)...",
				DbParameters.Empty, [DbParameters.FromDto(new { foo = 1 })], new BulkInsertSettings { MaxRowsPerBatch = 0 }).ToList())
			.Should().Throw<ArgumentException>();
	}

	[Test]
	public void NegativeBatchSize_Throws()
	{
		Invoking(() => BulkInsertUtility.GetBulkInsertCommands("VALUES (@foo)...",
				DbParameters.Empty, [DbParameters.FromDto(new { foo = 1 })], new BulkInsertSettings { MaxRowsPerBatch = -1 }).ToList())
			.Should().Throw<ArgumentException>();
	}

	[Test]
	public void MinimalInsert()
	{
		var commands = BulkInsertUtility.GetBulkInsertCommands("INSERT INTO t (foo)VALUES(@foo)...;",
			DbParameters.Empty, [DbParameters.FromDto(new { foo = 1 })]).ToList();
		commands.Count.Should().Be(1);
		commands[0].Sql.Should().Be("INSERT INTO t (foo)VALUES(@foo_0);");
		commands[0].Parameters.Enumerate().Should().Equal((Name: "foo_0", Value: 1));
	}

	[Test]
	public void InsertNotRequired()
	{
		var commands = BulkInsertUtility.GetBulkInsertCommands("VALUES (@foo)...",
			DbParameters.Empty, [DbParameters.FromDto(new { foo = 1 })]).ToList();
		commands.Count.Should().Be(1);
		commands[0].Sql.Should().Be("VALUES (@foo_0)");
		commands[0].Parameters.Enumerate().Should().Equal((Name: "foo_0", Value: 1));
	}

	[Test]
	public void MultipleInserts()
	{
		var commands = BulkInsertUtility.GetBulkInsertCommands("INSERT INTO t VALUES (@t); INSERT INTO u VALUES (@u)...; INSERT INTO v VALUES (@v);",
			DbParameters.Empty, [DbParameters.FromDto(new { t = 1, u = 2, v = 3 })]).ToList();
		commands.Count.Should().Be(1);
		commands[0].Sql.Should().Be("INSERT INTO t VALUES (@t); INSERT INTO u VALUES (@u_0); INSERT INTO v VALUES (@v);");
		commands[0].Parameters.Enumerate().Should().Equal((Name: "u_0", Value: 2));
	}

	[Test]
	public void CommonAndInsertedParameters()
	{
		var commands = BulkInsertUtility.GetBulkInsertCommands("VALUES (@a, @b, @c, @d)...",
			DbParameters.FromDto(new { a = 1, b = 2 }), [DbParameters.FromDto(new { c = 3, d = 4 }), DbParameters.FromDto(new { c = 5, d = 6 })]).ToList();
		commands.Count.Should().Be(1);
		commands[0].Sql.Should().Be("VALUES (@a, @b, @c_0, @d_0), (@a, @b, @c_1, @d_1)");
		commands[0].Parameters.Enumerate().Should().Equal(
			(Name: "a", Value: 1),
			(Name: "b", Value: 2),
			(Name: "c_0", Value: 3),
			(Name: "d_0", Value: 4),
			(Name: "c_1", Value: 5),
			(Name: "d_1", Value: 6));
	}

	[TestCase(3, null)]
	[TestCase(null, 6)]
	[TestCase(3, 10)]
	[TestCase(10, 6)]
	public void EightRowsInThreeBatches(int? maxRecordsPerBatch, int? maxParametersPerBatch)
	{
		var settings = new BulkInsertSettings
		{
			MaxRowsPerBatch = maxRecordsPerBatch,
			MaxParametersPerBatch = maxParametersPerBatch,
		};
		var commands = BulkInsertUtility.GetBulkInsertCommands("VALUES(@foo,@bar)...",
			DbParameters.Empty, Enumerable.Range(0, 8).Select(x => DbParameters.FromDto(new { foo = x, bar = x * 2 })), settings).ToList();
		commands.Count.Should().Be(3);
		commands[0].Sql.Should().Be("VALUES(@foo_0,@bar_0), (@foo_1,@bar_1), (@foo_2,@bar_2)");
		commands[0].Parameters.Enumerate().Should().Equal((Name: "foo_0", Value: 0), (Name: "bar_0", Value: 0), (Name: "foo_1", Value: 1), (Name: "bar_1", Value: 2), (Name: "foo_2", Value: 2), (Name: "bar_2", Value: 4));
		commands[1].Sql.Should().Be("VALUES(@foo_0,@bar_0), (@foo_1,@bar_1), (@foo_2,@bar_2)");
		commands[1].Parameters.Enumerate().Should().Equal((Name: "foo_0", Value: 3), (Name: "bar_0", Value: 6), (Name: "foo_1", Value: 4), (Name: "bar_1", Value: 8), (Name: "foo_2", Value: 5), (Name: "bar_2", Value: 10));
		commands[2].Sql.Should().Be("VALUES(@foo_0,@bar_0), (@foo_1,@bar_1)");
		commands[2].Parameters.Enumerate().Should().Equal((Name: "foo_0", Value: 6), (Name: "bar_0", Value: 12), (Name: "foo_1", Value: 7), (Name: "bar_1", Value: 14));
	}

	[Test]
	public void CaseInsensitiveValues()
	{
		var commands = BulkInsertUtility.GetBulkInsertCommands("VaLueS(@foo)...",
			DbParameters.Empty, [DbParameters.FromDto(new { foo = 1 })]).ToList();
		commands.Count.Should().Be(1);
		commands[0].Sql.Should().Be("VaLueS(@foo_0)");
		commands[0].Parameters.Enumerate().Should().Equal((Name: "foo_0", Value: 1));
	}

	[Test]
	public void CaseInsensitiveNames()
	{
		var commands = BulkInsertUtility.GetBulkInsertCommands("values (@foo, @Bar, @BAZ, @bam)...",
			DbParameters.Empty, [DbParameters.FromDto(new { Foo = 1, BAR = 2, baz = 3 })]).ToList();
		commands.Count.Should().Be(1);
		commands[0].Sql.Should().Be("values (@foo_0, @Bar_0, @BAZ_0, @bam)");
		commands[0].Parameters.Enumerate().Should().Equal((Name: "Foo_0", Value: 1), (Name: "BAR_0", Value: 2), (Name: "baz_0", Value: 3));
	}

	[Test]
	public void PunctuatedNames()
	{
		var commands = BulkInsertUtility.GetBulkInsertCommands("values (@foo, @bar)...",
			DbParameters.Empty, [DbParameters.Create(("@foo", 1), ("@Bar", 2))]).ToList();
		commands.Count.Should().Be(1);
		commands[0].Sql.Should().Be("values (@foo_0, @bar_0)");
		commands[0].Parameters.Enumerate().Should().Equal((Name: "@foo_0", Value: 1), (Name: "@Bar_0", Value: 2));
	}

	[Test]
	public void SubstringNames()
	{
		var commands = BulkInsertUtility.GetBulkInsertCommands("values (@a, @aa, @aaa, @aaaa)...",
			DbParameters.FromDto(new { a = 1, aaa = 3 }), [DbParameters.FromDto(new { aa = 2, aaaa = 4 })]).ToList();
		commands.Count.Should().Be(1);
		commands[0].Sql.Should().Be("values (@a, @aa_0, @aaa, @aaaa_0)");
		commands[0].Parameters.Enumerate().Should().Equal((Name: "a", Value: 1), (Name: "aaa", Value: 3), (Name: "aa_0", Value: 2), (Name: "aaaa_0", Value: 4));
	}

	[Test]
	public void WhitespaceEverywhere()
	{
		var commands = BulkInsertUtility.GetBulkInsertCommands("\r\n\t VALUES\n\t \r(\t \r\n@foo \r\n\t)\r\n\t ...\t\r\n",
			DbParameters.Empty, [DbParameters.FromDto(new { foo = 1 })]).ToList();
		commands.Count.Should().Be(1);
		commands[0].Sql.Should().Be("\r\n\t VALUES\n\t \r(\t \r\n@foo_0 \r\n\t)\t\r\n");
		commands[0].Parameters.Enumerate().Should().Equal((Name: "foo_0", Value: 1));
	}

	[Test]
	public void NothingToInsert()
	{
		var commands = BulkInsertUtility.GetBulkInsertCommands("VALUES(@foo)...", DbParameters.Empty, []).ToList();
		commands.Count.Should().Be(0);
	}

	[Test]
	public void NoParameterNameValidation()
	{
		var commands = BulkInsertUtility.GetBulkInsertCommands("VALUES (@a, @b, @c, @d)...",
			DbParameters.FromDto(new { e = 1, f = 2 }), [DbParameters.FromDto(new { g = 3, h = 4 }), DbParameters.FromDto(new { g = 5, h = 6 })]).ToList();
		commands.Count.Should().Be(1);
		commands[0].Sql.Should().Be("VALUES (@a, @b, @c, @d), (@a, @b, @c, @d)");
		commands[0].Parameters.Enumerate().Should().Equal(
			(Name: "e", Value: 1),
			(Name: "f", Value: 2));
	}

	[Test]
	public void ComplexValues()
	{
		var commands = BulkInsertUtility.GetBulkInsertCommands("VALUES (@a + (@d * @c) -\r\n\t@d)...",
			DbParameters.FromDto(new { a = 1, b = 2 }), [DbParameters.FromDto(new { c = 3, d = 4 }), DbParameters.FromDto(new { c = 5, d = 6 })]).ToList();
		commands.Count.Should().Be(1);
		commands[0].Sql.Should().Be("VALUES (@a + (@d_0 * @c_0) -\r\n\t@d_0), (@a + (@d_1 * @c_1) -\r\n\t@d_1)");
		commands[0].Parameters.Enumerate().Should().Equal(
			(Name: "a", Value: 1),
			(Name: "b", Value: 2),
			(Name: "c_0", Value: 3),
			(Name: "d_0", Value: 4),
			(Name: "c_1", Value: 5),
			(Name: "d_1", Value: 6));
	}

	[Test]
	public void DifferentParameters()
	{
		var commands = BulkInsertUtility.GetBulkInsertCommands("VALUES (@a, @b)...",
			DbParameters.FromDto(new { a = 1, b = 2 }),
			[DbParameters.FromDto(new { b = 4 }), DbParameters.Empty, DbParameters.FromDto(new { a = 3 })]).ToList();
		commands.Count.Should().Be(1);
		commands[0].Sql.Should().Be("VALUES (@a, @b_0), (@a, @b), (@a_2, @b)");
		commands[0].Parameters.Enumerate().Should().Equal(
			(Name: "a", Value: 1),
			(Name: "b", Value: 2),
			(Name: "b_0", Value: 4),
			(Name: "a_2", Value: 3));
	}

	private static DbConnector CreateConnector() => new(new SqliteConnection("Data Source=:memory:"));
}
