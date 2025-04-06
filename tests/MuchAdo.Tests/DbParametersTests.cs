using FluentAssertions;
using Microsoft.Data.Sqlite;
using NUnit.Framework;
using static FluentAssertions.FluentActions;

namespace MuchAdo.Tests;

[TestFixture]
internal sealed class DbParametersTests
{
	[Test]
	public void Empty()
	{
		DbParameters.Empty.Count.Should().Be(0);
	}

	[Test]
	public void CreateSingle()
	{
		DbParameters.Create("one", 1).Enumerate().Should().Equal(("one", 1));
	}

	[Test]
	public void CreateFromPairParams()
	{
		DbParameters.Create().Count.Should().Be(0);
		DbParameters.Create(("one", 1)).Enumerate().Should().Equal(("one", 1));
		DbParameters.Create(("one", 1), ("two", 2L)).Enumerate().Should().Equal(("one", 1L), ("two", 2L));
		DbParameters.Create<object>(("one", 1), ("two", 2L)).Enumerate().Should().Equal(("one", 1), ("two", 2L));
		DbParameters.Create<object?>(("one", 1), ("null", null)).Enumerate().Should().Equal(("one", 1), ("null", null));
	}

	[Test]
	public void CreateFromPairList()
	{
		DbParameters.Create([("one", "1"), ("two", "2")]).Enumerate().Should().Equal(("one", "1"), ("two", "2"));
		DbParameters.Create([("one", 1), ("two", 2L)]).Enumerate().Should().Equal(("one", 1L), ("two", 2L));
		var array1 = new (string, object)[] { ("one", 1), ("two", 2L) };
		DbParameters.Create(array1).Enumerate().Should().Equal(("one", 1), ("two", 2L));
		var array2 = new (string, object?)[] { ("one", 1), ("two", 2L) };
		DbParameters.Create(array2).Enumerate().Should().Equal(("one", 1), ("two", 2L));
		var array3 = new[] { ("one", (object) 1), ("two", 2L) };
		DbParameters.Create(array3).Enumerate().Should().Equal(("one", 1), ("two", 2L));
		var array4 = new[] { ("one", (object?) 1), ("two", 2L) };
		DbParameters.Create(array4).Enumerate().Should().Equal(("one", 1), ("two", 2L));
	}

	[Test]
	public void CreateFromDictionary()
	{
		DbParameters.Create(new Dictionary<string, long> { { "one", 1 }, { "two", 2L } }).Enumerate().Should().Equal(("one", 1L), ("two", 2L));
		DbParameters.Create(new Dictionary<string, int?> { { "one", 1 }, { "null", null } }).Enumerate().Should().Equal(("one", 1), ("null", null));
		DbParameters.Create(new Dictionary<string, object> { { "one", 1 }, { "two", 2L } }).Enumerate().Should().Equal(("one", 1), ("two", 2L));
		DbParameters.Create(new Dictionary<string, object?> { { "one", 1 }, { "null", null } }).Enumerate().Should().Equal(("one", 1), ("null", null));
	}

	[Test]
	public void CreateFromDto()
	{
		var parameters = DbParameters.Create(DbParameters.FromDto(new { one = 1 }), DbParameters.FromDto(new HasTwo()));
		parameters.Count.Should().Be(2);
		parameters.Enumerate().Should().Equal(("one", 1), ("Two", 2));
	}

	[Test]
	public void CreateFromDtoRenamed()
	{
		var parameters = DbParameters.FromDto(new { one = 1, Two = 2 }).Renamed(x => $"it's {x}");
		parameters.Count.Should().Be(2);
		parameters.Enumerate().Should().Equal(("it's one", 1), ("it's Two", 2));
	}

	[Test]
	public void CreateFromDtoWhere()
	{
		var parameters = DbParameters.FromDto(new { one = 1, two = 2, three = 3 }).Where(x => x[0] == 't');
		parameters.Count.Should().Be(2);
		parameters.Enumerate().Should().Equal(("two", 2), ("three", 3));
	}

	[Test]
	public void CreateFromDtoWhereRenamedWhereRenamed()
	{
		var parameters = DbParameters.FromDto(new { one = 1, Two = 2, three = 3 }).Where(x => x[0] == 't').Renamed(x => x.ToUpperInvariant()).Where(x => x[0] == 'T').Renamed(x => x.ToLowerInvariant());
		parameters.Count.Should().Be(1);
		parameters.Enumerate().Should().Equal(("three", 3));

		using var connection = new SqliteConnection("Data Source=:memory:");
		using var command = connection.CreateCommand();
		parameters.Apply(command, DbProviderMethods.Default);
		command.Parameters.Count.Should().Be(1);
		command.Parameters[0].ParameterName.Should().Be("three");
		command.Parameters[0].Value.Should().Be(3);

		parameters = DbParameters.FromDto(new { one = 10, Two = 20, three = 30 }).Where(x => x[0] == 't').Renamed(x => x.ToUpperInvariant()).Where(x => x[0] == 'T').Renamed(x => x.ToLowerInvariant());
		parameters.Count.Should().Be(1);
		parameters.Enumerate().Should().Equal(("three", 30));

		parameters.Reapply(command, 0, DbProviderMethods.Default);
		command.Parameters[0].ParameterName.Should().Be("three");
		command.Parameters[0].Value.Should().Be(30);
	}

	[Test]
	public void CreateFromDtoNamedWhereNamedWhere()
	{
		var parameters = DbParameters.FromDto(new { one = 1, Two = 2, three = 3 }).Renamed(x => x.ToUpperInvariant()).Where(x => x[0] == 'T').Renamed(x => x.ToLowerInvariant()).Where(x => x[0] == 't');
		parameters.Count.Should().Be(2);
		parameters.Enumerate().Should().Equal(("two", 2), ("three", 3));
	}

	[Test]
	public void Count()
	{
		DbParameters.Create(("one", 1)).Count.Should().Be(1);
	}

	[Test]
	public void Nulls()
	{
		Invoking(() => DbParameters.Create(null!)).Should().Throw<ArgumentNullException>();
		Invoking(() => DbParameters.Create(default((string, string)[])!)).Should().Throw<ArgumentNullException>();
		Invoking(() => DbParameters.Create(default(Dictionary<string, string>)!)).Should().Throw<ArgumentNullException>();
		Invoking(() => DbParameters.FromDto(default(object?))).Should().Throw<ArgumentNullException>();
	}

	private sealed class HasTwo
	{
		public int Two { get; } = 2;
	}
}
