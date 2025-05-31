using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using NUnit.Framework;
using static FluentAssertions.FluentActions;

namespace MuchAdo.Tests;

[TestFixture]
[SuppressMessage("Usage", "CA2263:Prefer generic overload when type is known", Justification = "Testing.")]
internal sealed class SqlSyntaxTests
{
	[Test]
	public void EmptySql()
	{
		var sql = Sql.Empty;
		var (text, parameters) = Render(sql);
		text.Should().Be("");
		parameters.EnumeratePairs().Should().Equal();
		sql.ToString().Should().Be("");
	}

	[TestCase("")]
	[TestCase("select * from widgets")]
	public void RawSql(string raw)
	{
		var sql = Sql.Raw(raw);
		var (text, parameters) = Render(sql);
		text.Should().Be(raw);
		parameters.EnumeratePairs().Should().Equal();
		sql.ToString().Should().Be(raw);
	}

	[Test]
	public void ParamSql()
	{
		var (text, parameters) = Render(Sql.Param("xyzzy"));
		text.Should().Be("@ado1");
		parameters.EnumeratePairs().Should().Equal(("ado1", "xyzzy"));
	}

	[Test]
	public void NamedParamSql()
	{
		var (text, parameters) = Render(Sql.NamedParam("abccb", "xyzzy"));
		text.Should().Be("@abccb");
		parameters.EnumeratePairs().Should().Equal(("abccb", "xyzzy"));
	}

	[Test]
	public void ListSql()
	{
		var (text, parameters) = Render(Sql.List(Sql.Param("one"), Sql.Param("two"), Sql.Raw("null")));
		text.Should().Be("@ado1, @ado2, null");
		parameters.EnumeratePairs().Should().Equal(("ado1", "one"), ("ado2", "two"));
	}

	[Test]
	public void ListNone()
	{
		var (text, parameters) = Render(Sql.List());
		text.Should().Be("");
		parameters.EnumeratePairs().Should().Equal();
	}

	[Test]
	public void ListEmpty()
	{
		var (text, parameters) = Render(Sql.List(Sql.Empty));
		text.Should().Be("");
		parameters.EnumeratePairs().Should().Equal();
	}

	[Test]
	public void TupleSql()
	{
		var (text, parameters) = Render(Sql.Tuple(Sql.Param("one"), Sql.Param("two"), Sql.Raw("null")));
		text.Should().Be("(@ado1, @ado2, null)");
		parameters.EnumeratePairs().Should().Equal(("ado1", "one"), ("ado2", "two"));
	}

	[Test]
	public void TupleNone()
	{
		var (text, parameters) = Render(Sql.Tuple());
		text.Should().Be("()");
		parameters.EnumeratePairs().Should().Equal();
	}

	[Test]
	public void TupleEmpty()
	{
		var (text, parameters) = Render(Sql.Tuple(Sql.Empty));
		text.Should().Be("()");
		parameters.EnumeratePairs().Should().Equal();
	}

	[Test]
	public void SetSql()
	{
		var (text, parameters) = Render(Sql.Set(Sql.Param("one"), Sql.Param("two"), Sql.Raw("null")));
		text.Should().Be("(@ado1, @ado2, null)");
		parameters.EnumeratePairs().Should().Equal(("ado1", "one"), ("ado2", "two"));
	}

	[Test]
	public void SetNone()
	{
		Invoking(() => Render(Sql.Set())).Should().Throw<InvalidOperationException>();
	}

	[Test]
	public void SetEmpty()
	{
		Invoking(() => Render(Sql.Set(Sql.Empty))).Should().Throw<InvalidOperationException>();
	}

	[Test]
	public void ParamsOneString()
	{
		var (text, parameters) = Render(Sql.Params("hi"));
		text.Should().Be("@ado1, @ado2");
		parameters.EnumeratePairs().Should().Equal(("ado1", 'h'), ("ado2", 'i'));
	}

	[Test]
	public void ParamsStrings()
	{
		var (text, parameters) = Render(Sql.Params(["one", "two", "three"]));
		text.Should().Be("@ado1, @ado2, @ado3");
		parameters.EnumeratePairs().Should().Equal(("ado1", "one"), ("ado2", "two"), ("ado3", "three"));
	}

	[Test]
	public void ParamsNumbers()
	{
		var (text, parameters) = Render(Sql.Params([1, 2]));
		text.Should().Be("@ado1, @ado2");
		parameters.EnumeratePairs().Should().Equal(("ado1", 1), ("ado2", 2));
	}

	[Test]
	public void ParamsParams()
	{
		var (text, parameters) = Render(Sql.Params([Sql.Param(1), Sql.NamedParam("two", 2), Sql.Params([3, 4])]));
		text.Should().Be("@ado1, @two, @ado2, @ado3");
		parameters.EnumeratePairs().Should().Equal(("ado1", 1), ("two", 2), ("ado2", 3), ("ado3", 4));
	}

	[Test]
	public void ParamsMixedNumbers()
	{
		var (text, parameters) = Render(Sql.Params<object>([1, 2L]));
		text.Should().Be("@ado1, @ado2");
		parameters.EnumeratePairs().Should().Equal(("ado1", 1), ("ado2", 2L));
	}

	[Test]
	public void ParamsMixedObjects()
	{
		var (text, parameters) = Render(Sql.Params<object?>(["one", 2, null, Sql.Params([3])]));
		text.Should().Be("@ado1, @ado2, @ado3, @ado4");
		parameters.EnumeratePairs().Should().Equal(("ado1", "one"), ("ado2", 2), ("ado3", null), ("ado4", 3));
	}

	[Test]
	public void ParamsNone()
	{
		var (text, parameters) = Render(Sql.Params<object?>([]));
		text.Should().Be("");
		parameters.EnumeratePairs().Should().Equal();
	}

	[Test]
	public void FormatEmpty()
	{
		var (text, parameters) = Render(Sql.Format($""));
		text.Should().Be("");
		parameters.EnumeratePairs().Should().Equal();
	}

	[Test]
	public void FormatNoArgs()
	{
		var (text, parameters) = Render(Sql.Format($"select * from widgets"));
		text.Should().Be("select * from widgets");
		parameters.EnumeratePairs().Should().Equal();
	}

	[Test]
	public void FormatImplicitParam()
	{
		var sql = Sql.Format($"select * from widgets where id in ({42}, {-42})");
		var (text, parameters) = Render(sql);
		text.Should().Be("select * from widgets where id in (@ado1, @ado2)");
		parameters.EnumeratePairs().Should().Equal(("ado1", 42), ("ado2", -42));
		sql.ToString().Should().Be("select * from widgets where id in (@ado1, @ado2)");
	}

	[TestCase(null)]
	[TestCase(42)]
	public void FormatSql(int? id)
	{
		var whereSql = id is null ? Sql.Empty : Sql.Format($"where id = {Sql.Param(id)}");
		var limit = 10;
		var (text, parameters) = Render(Sql.Format($"select * from {Sql.Raw("widgets")} {whereSql} limit {limit}"));
		if (id is null)
		{
			text.Should().Be("select * from widgets  limit @ado1");
			parameters.EnumeratePairs().Should().Equal(("ado1", limit));
		}
		else
		{
			text.Should().Be("select * from widgets where id = @ado1 limit @ado2");
			parameters.EnumeratePairs().Should().Equal(("ado1", id), ("ado2", limit));
		}
	}

	[Test]
	public void FormatSetStringsArray()
	{
		var strings = new[] { "one", "two" };
		var sql = Sql.Format($"select * from widgets where id in {strings:set}");
		var (text, parameters) = Render(sql);
		text.Should().Be("select * from widgets where id in (@ado1, @ado2)");
		parameters.EnumeratePairs().Should().Equal(("ado1", "one"), ("ado2", "two"));
	}

	[Test]
	public void FormatSetStringsEnumerable()
	{
		var strings = new[] { "one", "two" }.AsEnumerable();
		var sql = Sql.Format($"select * from widgets where id in {strings:set}");
		var (text, parameters) = Render(sql);
		text.Should().Be("select * from widgets where id in (@ado1, @ado2)");
		parameters.EnumeratePairs().Should().Equal(("ado1", "one"), ("ado2", "two"));
	}

	[Test]
	public void FormatSetStringsEmpty()
	{
		var strings = Array.Empty<string>();
		Invoking(() => Render(Sql.Format($"select * from widgets where id in {strings:set}"))).Should().Throw<InvalidOperationException>();
	}

	[Test]
	public void FormatSetSqls()
	{
		var sqls = new[] { Sql.Raw("one"), Sql.Raw("two") };
		Invoking(() => Sql.Format($"select * from widgets where id in {sqls:set}")).Should().Throw<NotSupportedException>();
	}

	[Test]
	public void FormatUnknown()
	{
		Invoking(() => Sql.Format($"select * from widgets where id in {"42":xyzzy}")).Should().Throw<NotSupportedException>();
	}

	[Test]
	public void SameParamTwice()
	{
		var id = 42;
		var name = "xyzzy";
		var nameParam = Sql.Param(name);
		var desc = "long description";
		var descParam = Sql.RepeatParam(desc);
		var (text, parameters) = Render(Sql.Format($"insert into widgets (Id, Name, Desc) values ({id}, {nameParam}, {descParam}) on duplicate key update Name = {nameParam}, Desc = {descParam}"));
		text.Should().Be("insert into widgets (Id, Name, Desc) values (@ado1, @ado2, @ado3) on duplicate key update Name = @ado4, Desc = @ado3");
		parameters.EnumeratePairs().Should().Equal(("ado1", id), ("ado2", name), ("ado3", desc), ("ado4", name));
	}

	[Test]
	public void JoinParams()
	{
		var (text, parameters) = Render(Sql.List(Sql.Param(42), Sql.Param(-42)));
		text.Should().Be("@ado1, @ado2");
		parameters.EnumeratePairs().Should().Equal(("ado1", 42), ("ado2", -42));
	}

	[Test]
	public void JoinEnumerable()
	{
		Render(CreateSql(42, 24)).Text.Should().Be("select * from widgets where width = @ado1 and height = @ado2;");
		Render(CreateSql(null, 24)).Text.Should().Be("select * from widgets where height = @ado1;");
		Render(CreateSql(null, null)).Text.Should().Be("select * from widgets ;");

		SqlSource CreateSql(int? width, int? height)
		{
			var sqls = new List<SqlSource>();
			if (width is not null)
				sqls.Add(Sql.Format($"width = {width}"));
			if (height is not null)
				sqls.Add(Sql.Format($"height = {height}"));
			var whereSql = sqls.Count == 0 ? Sql.Empty : Sql.Format($"where {Sql.Join(" and ", sqls)}");
			return Sql.Format($"select * from widgets {whereSql};");
		}
	}

	[Test]
	public void JoinEmpty()
	{
		var (text, parameters) = Render(Sql.Join("/", Sql.Raw("one"), Sql.Empty, Sql.Raw("two")));
		text.Should().Be("one/two");
		parameters.EnumeratePairs().Should().Equal();
	}

	[Test]
	public void AddFragments()
	{
		var (text, parameters) = Render(Sql.Format($"select {1};") + Sql.Format($"select {2};"));
		text.Should().Be("select @ado1;select @ado2;");
		parameters.EnumeratePairs().Should().Equal(("ado1", 1), ("ado2", 2));
	}

	[Test]
	public void ConcatParams()
	{
		var (text, parameters) = Render(Sql.Concat(Sql.Format($"select {1};"), Sql.Format($"select {2};")));
		text.Should().Be("select @ado1;select @ado2;");
		parameters.EnumeratePairs().Should().Equal(("ado1", 1), ("ado2", 2));
	}

	[Test]
	public void ConcatEnumerable()
	{
		var (text, parameters) = Render(Sql.Concat(Enumerable.Range(1, 2).Select(x => Sql.Format($"select {x};"))));
		text.Should().Be("select @ado1;select @ado2;");
		parameters.EnumeratePairs().Should().Equal(("ado1", 1), ("ado2", 2));
	}

	[Test]
	public void LikeParamStartsWithSql()
	{
		var (text, parameters) = Render(Sql.LikeParamStartsWith("xy_zy"));
		text.Should().Be("@ado1");
		parameters.EnumeratePairs().Should().Equal(("ado1", "xy\\_zy%"));
	}

	[Test]
	public void NameSql()
	{
		Invoking(() => Render(Sql.Name("xyzzy"), SqlSyntax.Default)).Should().Throw<InvalidOperationException>();
		var sql = Sql.Name("x`y[z]z\"y");
		Render(sql, SqlSyntax.MySql).Text.Should().Be("`x``y[z]z\"y`");
		Render(sql, SqlSyntax.Postgres).Text.Should().Be("\"x`y[z]z\"\"y\"");
		Render(sql, SqlSyntax.SqlServer).Text.Should().Be("[x`y[z]]z\"y]");
		Render(sql, SqlSyntax.Sqlite).Text.Should().Be("\"x`y[z]z\"\"y\"");
		sql.ToString().Should().Be("\"x`y[z]z\"\"y\"");
	}

	[Test]
	public void ColumnNamesAndValuesSql()
	{
		var syntax = SqlSyntax.Ansi;

		Render(Sql.DtoColumnNames<ItemDto>(), syntax).Text.Should().Be("""
			"ItemId", "DisplayName", "IsActive"
			""");

		var item = new ItemDto { Id = 3, DisplayName = "three" };
		var (text, parameters) = Render(Sql.Format($"insert into Items ({Sql.DtoColumnNames<ItemDto>()}) values ({Sql.DtoParams(item)});"), syntax);
		text.Should().Be("""insert into Items ("ItemId", "DisplayName", "IsActive") values (@ado1, @ado2, @ado3);""");
		parameters.EnumeratePairs().Should().Equal(("ado1", item.Id), ("ado2", item.DisplayName), ("ado3", item.IsActive));
	}

	[Test]
	public void TableColumnNamesAndValuesSql()
	{
		var syntax = SqlSyntax.MySql;
		Render(Sql.DtoColumnNames<ItemDto>().From("t"), syntax).Text.Should().Be("`t`.`ItemId`, `t`.`DisplayName`, `t`.`IsActive`");
	}

	[Test]
	public void SnakeCaseNamesAndValuesSql()
	{
		var syntax = SqlSyntax.MySql.WithSnakeCaseColumnNames();
		Render(Sql.DtoColumnNames<ItemDto>().From("t"), syntax).Text.Should().Be("`t`.`ItemId`, `t`.`display_name`, `t`.`is_active`");
	}

	[Test]
	public void ColumnNamesAndValuesWhereSql()
	{
		var syntax = SqlSyntax.Ansi;

		Render(Sql.DtoColumnNames<ItemDto>().Where(x => x is nameof(ItemDto.DisplayName)), syntax).Text.Should().Be("\"DisplayName\"");

		var item = new ItemDto { Id = 3, DisplayName = "three" };
		var (text, parameters) = Render(Sql.Format($"""
			insert into Items ({Sql.DtoColumnNames(item).Where(x => x is nameof(ItemDto.DisplayName))})
			values ({Sql.DtoParams(item).Where(x => x is nameof(ItemDto.DisplayName))});
			"""), syntax);
		text.Should().Be("""
			insert into Items ("DisplayName")
			values (@ado1);
			""");
		parameters.EnumeratePairs().Should().Equal(("ado1", item.DisplayName));
	}

	[Test]
	public void ColumnNamesAndDtoParamNamesWhereSql()
	{
		var syntax = SqlSyntax.MySql;

		var item = new ItemDto { Id = 3, DisplayName = "three" };
		var (text, parameters) = Render(Sql.Format($"""
			insert into Items ({Sql.DtoColumnNames(item).Where(x => x is nameof(ItemDto.DisplayName))})
			values ({Sql.DtoParamNames(item).Where(x => x is nameof(ItemDto.DisplayName))});
			"""), syntax);
		text.Should().Be("""
			insert into Items (`DisplayName`)
			values (@DisplayName);
			""");
		parameters.EnumeratePairs().Should().BeEmpty();
	}

	[Test]
	public void ColumnNamesAndValuesWhereNoneException()
	{
		var syntax = SqlSyntax.MySql;

		Render(Sql.DtoColumnNames<ItemDto>().Where(_ => false), syntax).Text.Should().Be("");
		Render(Sql.DtoParams(new ItemDto()).Where(_ => false), syntax).Text.Should().Be("");
	}

	[Test]
	public void DtoParamNamesSql()
	{
		var syntax = SqlSyntax.MySql;

		Render(Sql.DtoParamNames<ItemDto>(), syntax).Text.Should().Be("@Id, @DisplayName, @IsActive");
		Render(Sql.DtoParamNames<ItemDto>().Renamed(x => x + "_"), syntax).Text.Should().Be("@Id_, @DisplayName_, @IsActive_");
		Render(Sql.DtoParamNames<ItemDto>().Renamed(x => x + "_").Renamed(x => x + "!"), syntax).Text.Should().Be("@Id_!, @DisplayName_!, @IsActive_!");
	}

	[Test]
	public void DtoParamNamesWhereSql()
	{
		var syntax = SqlSyntax.MySql;

		Render(Sql.DtoParamNames<ItemDto>().Where(NotId), syntax).Text.Should().Be("@DisplayName, @IsActive");
		Render(Sql.DtoParamNames<ItemDto>().Where(NotId).Where(x => x is not "DisplayName"), syntax).Text.Should().Be("@IsActive");
		Render(Sql.DtoParamNames<ItemDto>().Where(NotId).Renamed(x => x + "_"), syntax).Text.Should().Be("@DisplayName_, @IsActive_");
		Render(Sql.DtoParamNames<ItemDto>().Renamed(x => x + "_").Where(NotId), syntax).Text.Should().Be("@Id_, @DisplayName_, @IsActive_");
		Render(Sql.DtoParamNames<ItemDto>().Where(NotId).Renamed(x => x + "_").Where(x => x is not "DisplayName_"), syntax).Text.Should().Be("@IsActive_");
		Render(Sql.DtoParamNames<ItemDto>().Renamed(x => x + "_").Where(x => x is not "DisplayName_").Renamed(x => x + "!"), syntax).Text.Should().Be("@Id_!, @IsActive_!");

		static bool NotId(string x) => x != nameof(ItemDto.Id);
	}

	[TestCase("", "")]
	[TestCase("one", "one")]
	[TestCase("one,two", "(one AND two)")]
	[TestCase("one,two,three", "(one and two and three)", true)]
	public void And(string values, string sql, bool lowercase = false)
	{
		var syntax = lowercase ? SqlSyntax.Default.WithLowercaseKeywords() : SqlSyntax.Default;
		var (text, parameters) = Render(Sql.And(values.Split([','], StringSplitOptions.RemoveEmptyEntries).Select(Sql.Raw)), syntax);
		text.Should().Be(sql);
		parameters.EnumeratePairs().Should().Equal();
	}

	[TestCase("", "")]
	[TestCase("one", "one")]
	[TestCase("one,two", "(one OR two)")]
	[TestCase("one,two,three", "(one or two or three)", true)]
	public void Or(string values, string sql, bool lowercase = false)
	{
		var syntax = lowercase ? SqlSyntax.Default.WithLowercaseKeywords() : SqlSyntax.Default;
		var (text, parameters) = Render(Sql.Or(values.Split([','], StringSplitOptions.RemoveEmptyEntries).Select(Sql.Raw)), syntax);
		text.Should().Be(sql);
		parameters.EnumeratePairs().Should().Equal();
	}

	[Test]
	public void AndOrAnd()
	{
		var (text, parameters) = Render(Sql.And(Sql.Raw("one"), Sql.Or(Sql.Raw("two"), Sql.And(Sql.Raw("three")))));
		text.Should().Be("(one AND (two OR three))");
		parameters.EnumeratePairs().Should().Equal();
	}

	[TestCase("", "")]
	[TestCase("true", "WHERE true")]
	[TestCase("true", "where true", true)]
	public void Where(string condition, string sql, bool lowercase = false)
	{
		var syntax = lowercase ? SqlSyntax.Default.WithLowercaseKeywords() : SqlSyntax.Default;
		var (text, parameters) = Render(Sql.Where(Sql.Raw(condition)), syntax);
		text.Should().Be(sql);
		parameters.EnumeratePairs().Should().Equal();
	}

	[TestCase("", "")]
	[TestCase("x asc", "ORDER BY x asc")]
	[TestCase("x asc;y desc", "order by x asc, y desc", true)]
	public void OrderBy(string columns, string sql, bool lowercase = false)
	{
		var syntax = lowercase ? SqlSyntax.Default.WithLowercaseKeywords() : SqlSyntax.Default;
		var (text, parameters) = Render(Sql.OrderBy(columns.Split([';'], StringSplitOptions.RemoveEmptyEntries).Select(Sql.Raw)), syntax);
		text.Should().Be(sql);
		parameters.EnumeratePairs().Should().Equal();
	}

	[TestCase("", "")]
	[TestCase("x", "GROUP BY x")]
	[TestCase("x;y", "group by x, y", true)]
	public void GroupBy(string columns, string sql, bool lowercase = false)
	{
		var syntax = lowercase ? SqlSyntax.Default.WithLowercaseKeywords() : SqlSyntax.Default;
		var (text, parameters) = Render(Sql.GroupBy(columns.Split([';'], StringSplitOptions.RemoveEmptyEntries).Select(Sql.Raw)), syntax);
		text.Should().Be(sql);
		parameters.EnumeratePairs().Should().Equal();
	}

	[TestCase("", "")]
	[TestCase("x < 1", "HAVING x < 1")]
	[TestCase("x < 1", "having x < 1", true)]
	public void Having(string condition, string sql, bool lowercase = false)
	{
		var syntax = lowercase ? SqlSyntax.Default.WithLowercaseKeywords() : SqlSyntax.Default;
		var (text, parameters) = Render(Sql.Having(Sql.Raw(condition)), syntax);
		text.Should().Be(sql);
		parameters.EnumeratePairs().Should().Equal();
	}

	[Test]
	public void Clauses()
	{
		var sql = Sql.Clauses(Sql.Raw("select *"), Sql.Raw("from Widgets"));
		sql.ToString().Should().Be("select *\nfrom Widgets");
	}

	private static (string Text, SqlParamSource Parameters) Render(SqlSource sql, SqlSyntax? syntax = null)
	{
		var target = new ParamTarget();
		var commandBuilder = new DbConnectorCommandBuilder(syntax ?? SqlSyntax.Default, true, target);
		sql.Render(commandBuilder);
		return (commandBuilder.GetText(), target.Params);
	}

	private sealed class ItemDto
	{
		[Column("ItemId")]
		public int Id { get; set; }

		public string? DisplayName { get; set; }

		public bool IsActive { get; set; }
	}

	private sealed class ParamTarget : ISqlParamTarget
	{
		public SqlParamSources Params { get; } = new();

		public void AcceptParameter<T>(string name, T value, SqlParamType? type) => Params.Add(Sql.NamedParam(name, value, type));
	}
}
