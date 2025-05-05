using FluentAssertions;
using NUnit.Framework;
using static FluentAssertions.FluentActions;

namespace MuchAdo.Tests;

[TestFixture]
internal sealed class SqlParamSourceTests
{
	[Test]
	public void Empty()
	{
		Sql.Empty.Enumerate().Should().Equal();
	}

	[Test]
	public void CreateSingle()
	{
		Sql.NamedParam("one", 1).EnumeratePairs().Should().Equal(("one", 1));
	}

	[Test]
	public void CreateFromPairParams()
	{
		Sql.NamedParams(("one", 1)).EnumeratePairs().Should().Equal(("one", 1));
		Sql.NamedParams(("one", 1), ("two", 2L)).EnumeratePairs().Should().Equal(("one", 1L), ("two", 2L));
		Sql.NamedParams<object>(("one", 1), ("two", 2L)).EnumeratePairs().Should().Equal(("one", 1), ("two", 2L));
		Sql.NamedParams<object?>(("one", 1), ("null", null)).EnumeratePairs().Should().Equal(("one", 1), ("null", null));
	}

	[Test]
	public void CreateFromPairList()
	{
		Sql.NamedParams([("one", "1"), ("two", "2")]).EnumeratePairs().Should().Equal(("one", "1"), ("two", "2"));
		Sql.NamedParams([("one", 1), ("two", 2L)]).EnumeratePairs().Should().Equal(("one", 1L), ("two", 2L));
		var array1 = new (string, object)[] { ("one", 1), ("two", 2L) };
		Sql.NamedParams(array1).EnumeratePairs().Should().Equal(("one", 1), ("two", 2L));
		var array2 = new (string, object?)[] { ("one", 1), ("two", 2L) };
		Sql.NamedParams(array2).EnumeratePairs().Should().Equal(("one", 1), ("two", 2L));
		var array3 = new[] { ("one", (object) 1), ("two", 2L) };
		Sql.NamedParams(array3).EnumeratePairs().Should().Equal(("one", 1), ("two", 2L));
		var array4 = new[] { ("one", (object?) 1), ("two", 2L) };
		Sql.NamedParams(array4).EnumeratePairs().Should().Equal(("one", 1), ("two", 2L));
	}

	[Test]
	public void CreateFromDictionary()
	{
		Sql.NamedParams(new Dictionary<string, long> { { "one", 1 }, { "two", 2L } }).EnumeratePairs().Should().Equal(("one", 1L), ("two", 2L));
		Sql.NamedParams(new Dictionary<string, int?> { { "one", 1 }, { "null", null } }).EnumeratePairs().Should().Equal(("one", 1), ("null", null));
		Sql.NamedParams(new Dictionary<string, object> { { "one", 1 }, { "two", 2L } }).EnumeratePairs().Should().Equal(("one", 1), ("two", 2L));
		Sql.NamedParams(new Dictionary<string, object?> { { "one", 1 }, { "null", null } }).EnumeratePairs().Should().Equal(("one", 1), ("null", null));
	}

	[Test]
	public void CreateFromDto()
	{
		var parameters = new SqlParamSources(Sql.DtoNamedParams(new { one = 1 }), Sql.DtoNamedParams(new HasTwo()));
		parameters.EnumeratePairs().Should().Equal(("one", 1), ("Two", 2));
	}

	[Test]
	public void CreateFromDtoRenamed()
	{
		var parameters = Sql.DtoNamedParams(new { one = 1, Two = 2 }).Renamed(x => $"it's {x}");
		parameters.EnumeratePairs().Should().Equal(("it's one", 1), ("it's Two", 2));
	}

	[Test]
	public void CreateFromDtoWhere()
	{
		var parameters = Sql.DtoNamedParams(new { one = 1, two = 2, three = 3 }).Where(x => x[0] == 't');
		parameters.EnumeratePairs().Should().Equal(("two", 2), ("three", 3));
	}

	[Test]
	public void CreateFromDtoWhereRenamedWhereRenamed()
	{
		var parameters = Sql.DtoNamedParams(new { one = 1, Two = 2, three = 3 }).Where(x => x[0] == 't').Renamed(x => x.ToUpperInvariant()).Where(x => x[0] == 'T').Renamed(x => x.ToLowerInvariant());
		parameters.EnumeratePairs().Should().Equal(("three", 3));

		parameters = Sql.DtoNamedParams(new { one = 10, Two = 20, three = 30 }).Where(x => x[0] == 't').Renamed(x => x.ToUpperInvariant()).Where(x => x[0] == 'T').Renamed(x => x.ToLowerInvariant());
		parameters.EnumeratePairs().Should().Equal(("three", 30));
	}

	[Test]
	public void CreateFromDtoNamedWhereNamedWhere()
	{
		var parameters = Sql.DtoNamedParams(new { one = 1, Two = 2, three = 3 }).Renamed(x => x.ToUpperInvariant()).Where(x => x[0] == 'T').Renamed(x => x.ToLowerInvariant()).Where(x => x[0] == 't');
		parameters.EnumeratePairs().Should().Equal(("two", 2), ("three", 3));
	}

	[Test]
	public void Nulls()
	{
		Invoking(() => new SqlParamSources(default(IEnumerable<SqlParamSource>)!)).Should().Throw<ArgumentNullException>();
		////Invoking(() => new SqlParamSources(default((string, string)[])!)).Should().Throw<ArgumentNullException>();
		////Invoking(() => new SqlParamSources(default(Dictionary<string, string>)!)).Should().Throw<ArgumentNullException>();
		////Invoking(() => Sql.NamedParamsFromDto(default(object?))).Should().Throw<ArgumentNullException>();
	}

	private sealed class HasTwo
	{
		public int Two { get; } = 2;
	}
}
