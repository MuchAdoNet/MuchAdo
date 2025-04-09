using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using MuchAdo.SqlFormatting;
using NUnit.Framework;
using static FluentAssertions.FluentActions;

namespace MuchAdo.Tests.SqlFormatting;

#pragma warning disable FL0014 // Interpolated strings for literals

[TestFixture]
[SuppressMessage("Usage", "CA2263:Prefer generic overload when type is known", Justification = "Testing.")]
internal sealed class SqlSyntaxTests
{
	[Test]
	public void NullSqlThrows()
	{
		Invoking(() => Render(null!)).Should().Throw<ArgumentNullException>();
	}

	[Test]
	public void EmptySql()
	{
		var sql = Sql.Empty;
		var (text, parameters) = Render(sql);
		text.Should().Be("");
		parameters.Count.Should().Be(0);
		sql.ToString().Should().Be("");
	}

	[TestCase("")]
	[TestCase("select * from widgets")]
	public void RawSql(string raw)
	{
		var sql = Sql.Raw(raw);
		var (text, parameters) = Render(sql);
		text.Should().Be(raw);
		parameters.Count.Should().Be(0);
		sql.ToString().Should().Be(raw);
	}

	[Test]
	public void ParamSql()
	{
		var (text, parameters) = Render(Sql.Param("xyzzy"));
		text.Should().Be("@ado0");
		parameters.Enumerate().Should().Equal(("ado0", "xyzzy"));
	}

	[Test]
	public void NamedParamSql()
	{
		var (text, parameters) = Render(Sql.NamedParam("abccb", "xyzzy"));
		text.Should().Be("@abccb");
		parameters.Enumerate().Should().Equal(("abccb", "xyzzy"));
	}

	[Test]
	public void ParamOfSql()
	{
		Invoking(() => Render(Sql.Param(Sql.Raw("xyzzy")))).Should().Throw<ArgumentException>();
		Invoking(() => Render(Sql.NamedParam("abccb", Sql.Raw("xyzzy")))).Should().Throw<ArgumentException>();
	}

	[Test]
	public void ListSql()
	{
		var (text, parameters) = Render(Sql.List(Sql.Param("one"), Sql.Param("two"), Sql.Raw("null")));
		text.Should().Be("@ado0, @ado1, null");
		parameters.Enumerate().Should().Equal(("ado0", "one"), ("ado1", "two"));
	}

	[Test]
	public void ListNone()
	{
		Invoking(() => Render(Sql.List())).Should().Throw<InvalidOperationException>();
		Invoking(() => Render(Sql.List(Sql.Empty))).Should().Throw<InvalidOperationException>();
	}

	[Test]
	public void TupleSql()
	{
		var (text, parameters) = Render(Sql.Tuple(Sql.Param("one"), Sql.Param("two"), Sql.Raw("null")));
		text.Should().Be("(@ado0, @ado1, null)");
		parameters.Enumerate().Should().Equal(("ado0", "one"), ("ado1", "two"));
	}

	[Test]
	public void TupleNone()
	{
		Invoking(() => Render(Sql.Tuple())).Should().Throw<InvalidOperationException>();
		Invoking(() => Render(Sql.Tuple(Sql.Empty))).Should().Throw<InvalidOperationException>();
	}

	[Test]
	public void ParamListSqlStrings()
	{
		var (text, parameters) = Render(Sql.ParamList(["one", "two", "three"]));
		text.Should().Be("@ado0, @ado1, @ado2");
		parameters.Enumerate().Should().Equal(("ado0", "one"), ("ado1", "two"), ("ado2", "three"));
	}

	[Test]
	public void ParamListSqlNumbers()
	{
		var (text, parameters) = Render(Sql.ParamList([1, 2]));
		text.Should().Be("@ado0, @ado1");
		parameters.Enumerate().Should().Equal(("ado0", 1), ("ado1", 2));
	}

	[Test]
	public void ParamListSqlMixedNumbers()
	{
		var (text, parameters) = Render(Sql.ParamList<object>([1, 2L]));
		text.Should().Be("@ado0, @ado1");
		parameters.Enumerate().Should().Equal(("ado0", 1), ("ado1", 2L));
	}

	[Test]
	public void ParamListSqlMixedObjects()
	{
		var (text, parameters) = Render(Sql.ParamList<object?>(["one", 2, null]));
		text.Should().Be("@ado0, @ado1, @ado2");
		parameters.Enumerate().Should().Equal(("ado0", "one"), ("ado1", 2), ("ado2", null));
	}

	[Test]
	public void ParamListNone()
	{
		Invoking(() => Render(Sql.ParamList<object?>([]))).Should().Throw<InvalidOperationException>();
	}

	[Test]
	public void ParamTupleSqlStrings()
	{
		var (text, parameters) = Render(Sql.ParamTuple(["one", "two", "three"]));
		text.Should().Be("(@ado0, @ado1, @ado2)");
		parameters.Enumerate().Should().Equal(("ado0", "one"), ("ado1", "two"), ("ado2", "three"));
	}

	[Test]
	public void ParamTupleSqlNumbers()
	{
		var (text, parameters) = Render(Sql.ParamTuple([1, 2]));
		text.Should().Be("(@ado0, @ado1)");
		parameters.Enumerate().Should().Equal(("ado0", 1), ("ado1", 2));
	}

	[Test]
	public void ParamTupleSqlMixedNumbers()
	{
		var (text, parameters) = Render(Sql.ParamTuple<object>([1, 2L]));
		text.Should().Be("(@ado0, @ado1)");
		parameters.Enumerate().Should().Equal(("ado0", 1), ("ado1", 2L));
	}

	[Test]
	public void ParamTupleSqlMixedObjects()
	{
		var (text, parameters) = Render(Sql.ParamTuple<object?>(["one", 2, null]));
		text.Should().Be("(@ado0, @ado1, @ado2)");
		parameters.Enumerate().Should().Equal(("ado0", "one"), ("ado1", 2), ("ado2", null));
	}

	[Test]
	public void ParamTupleNone()
	{
		Invoking(() => Render(Sql.ParamTuple<object?>([]))).Should().Throw<InvalidOperationException>();
	}

	[Test]
	public void FormatEmpty()
	{
		var (text, parameters) = Render(Sql.Format($""));
		text.Should().Be("");
		parameters.Count.Should().Be(0);
	}

	[Test]
	public void FormatNoArgs()
	{
		var (text, parameters) = Render(Sql.Format($"select * from widgets"));
		text.Should().Be("select * from widgets");
		parameters.Count.Should().Be(0);
	}

	[Test]
	public void FormatImplicitParam()
	{
		var sql = Sql.Format($"select * from widgets where id in ({42}, {-42})");
		var (text, parameters) = Render(sql);
		text.Should().Be("select * from widgets where id in (@ado0, @ado1)");
		parameters.Enumerate().Should().Equal(("ado0", 42), ("ado1", -42));
		sql.ToString().Should().Be("select * from widgets where id in (@ado0, @ado1)");
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
			text.Should().Be("select * from widgets  limit @ado0");
			parameters.Enumerate().Should().Equal(("ado0", limit));
		}
		else
		{
			text.Should().Be("select * from widgets where id = @ado0 limit @ado1");
			parameters.Enumerate().Should().Equal(("ado0", id), ("ado1", limit));
		}
	}

	[Test]
	public void SameParamTwice()
	{
		var id = 42;
		var name = "xyzzy";
		var desc = "long description";
		var descParam = Sql.Param(desc);
		var (text, parameters) = Render(Sql.Format($"insert into widgets (Id, Name, Desc) values ({id}, {name}, {descParam}) on duplicate key update Name = {name}, Desc = {descParam}"));
		text.Should().Be("insert into widgets (Id, Name, Desc) values (@ado0, @ado1, @ado2) on duplicate key update Name = @ado3, Desc = @ado2");
		parameters.Enumerate().Should().Equal(("ado0", id), ("ado1", name), ("ado2", desc), ("ado3", name));
	}

	[Test]
	public void JoinParams()
	{
		var (text, parameters) = Render(Sql.Join(", ", Sql.Param(42), Sql.Param(-42)));
		text.Should().Be("@ado0, @ado1");
		parameters.Enumerate().Should().Equal(("ado0", 42), ("ado1", -42));
	}

	[Test]
	public void JoinEnumerable()
	{
		Render(CreateSql(42, 24)).Text.Should().Be("select * from widgets where width = @ado0 and height = @ado1;");
		Render(CreateSql(null, 24)).Text.Should().Be("select * from widgets where height = @ado0;");
		Render(CreateSql(null, null)).Text.Should().Be("select * from widgets ;");

		Sql CreateSql(int? width, int? height)
		{
			var sqls = new List<Sql>();
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
		parameters.Count.Should().Be(0);
	}

	[Test]
	public void AddFragments()
	{
		var (text, parameters) = Render(Sql.Format($"select {1};") + Sql.Format($"select {2};"));
		text.Should().Be("select @ado0;select @ado1;");
		parameters.Enumerate().Should().Equal(("ado0", 1), ("ado1", 2));
	}

	[Test]
	public void ConcatParams()
	{
		var (text, parameters) = Render(Sql.Concat(Sql.Format($"select {1};"), Sql.Format($"select {2};")));
		text.Should().Be("select @ado0;select @ado1;");
		parameters.Enumerate().Should().Equal(("ado0", 1), ("ado1", 2));
	}

	[Test]
	public void ConcatEnumerable()
	{
		var (text, parameters) = Render(Sql.Concat(Enumerable.Range(1, 2).Select(x => Sql.Format($"select {x};"))));
		text.Should().Be("select @ado0;select @ado1;");
		parameters.Enumerate().Should().Equal(("ado0", 1), ("ado1", 2));
	}

	[Test]
	public void LikeParamStartsWithSql()
	{
		var (text, parameters) = Render(Sql.LikeParamStartsWith("xy_zy"));
		text.Should().Be("@ado0");
		parameters.Enumerate().Should().Equal(("ado0", "xy\\_zy%"));
	}

	[Test]
	public void NameSql()
	{
		Invoking(() => SqlSyntax.Default.Render(Sql.Name("xyzzy"))).Should().Throw<InvalidOperationException>();
		var sql = Sql.Name("x`y[z]z\"y");
		SqlSyntax.MySql.Render(sql).Text.Should().Be("`x``y[z]z\"y`");
		SqlSyntax.Postgres.Render(sql).Text.Should().Be("\"x`y[z]z\"\"y\"");
		SqlSyntax.SqlServer.Render(sql).Text.Should().Be("[x`y[z]]z\"y]");
		SqlSyntax.Sqlite.Render(sql).Text.Should().Be("\"x`y[z]z\"\"y\"");
		sql.ToString().Should().Be("\"x`y[z]z\"\"y\"");
	}

	[Test]
	public void ColumnNamesAndValuesSql()
	{
		var syntax = SqlSyntax.MySql;

		syntax.Render(Sql.ColumnNames<ItemDto>()).Text.Should().Be("`ItemId`, `DisplayName`, `IsActive`");

		var item = new ItemDto { Id = 3, DisplayName = "three" };
		var (text, parameters) = syntax.Render(Sql.Format($"insert into Items ({Sql.ColumnNames<ItemDto>()}) values ({Sql.ColumnParams(item)});"));
		text.Should().Be("insert into Items (`ItemId`, `DisplayName`, `IsActive`) values (@ado0, @ado1, @ado2);");
		parameters.Enumerate().Should().Equal(("ado0", item.Id), ("ado1", item.DisplayName), ("ado2", item.IsActive));
	}

	[Test]
	public void TableColumnNamesAndValuesSql()
	{
		var syntax = SqlSyntax.MySql;
		syntax.Render(Sql.ColumnNames<ItemDto>().From("t")).Text.Should().Be("`t`.`ItemId`, `t`.`DisplayName`, `t`.`IsActive`");
	}

	[Test]
	public void SnakeCaseNamesAndValuesSql()
	{
		var syntax = SqlSyntax.MySql.WithSnakeCaseColumnNames();
		syntax.Render(Sql.ColumnNames<ItemDto>().From("t")).Text.Should().Be("`t`.`ItemId`, `t`.`display_name`, `t`.`is_active`");
	}

	[Test]
	public void ColumnNamesAndValuesWhereSql()
	{
		var syntax = SqlSyntax.MySql;

		syntax.Render(Sql.ColumnNames<ItemDto>().Where(x => x is nameof(ItemDto.DisplayName))).Text.Should().Be("`DisplayName`");

		var item = new ItemDto { Id = 3, DisplayName = "three" };
		var (text, parameters) = syntax.Render(Sql.Format($"""
			insert into Items ({Sql.ColumnNames(item).Where(x => x is nameof(ItemDto.DisplayName))})
			values ({Sql.ColumnParams(item).Where(x => x is nameof(ItemDto.DisplayName))});
			"""));
		text.Should().Be("""
			insert into Items (`DisplayName`)
			values (@ado0);
			""");
		parameters.Enumerate().Should().Equal(("ado0", item.DisplayName));
	}

	[Test]
	public void ColumnNamesAndDtoParamNamesWhereSql()
	{
		var syntax = SqlSyntax.MySql;

		var item = new ItemDto { Id = 3, DisplayName = "three" };
		var (text, parameters) = syntax.Render(Sql.Format($"""
			insert into Items ({Sql.ColumnNames(item).Where(x => x is nameof(ItemDto.DisplayName))})
			values ({Sql.DtoParamNames(item).Where(x => x is nameof(ItemDto.DisplayName))});
			"""));
		text.Should().Be("""
			insert into Items (`DisplayName`)
			values (@DisplayName);
			""");
		parameters.Enumerate().Should().BeEmpty();
	}

	[Test]
	public void ColumnNamesAndValuesWhereNoneException()
	{
		var syntax = SqlSyntax.MySql;

		Invoking(() => syntax.Render(Sql.ColumnNames<ItemDto>().Where(_ => false))).Should().Throw<InvalidOperationException>();
		Invoking(() => syntax.Render(Sql.ColumnParams(new ItemDto()).Where(_ => false))).Should().Throw<InvalidOperationException>();
	}

	[Test]
	public void DtoParamNamesSql()
	{
		var syntax = SqlSyntax.MySql;

		syntax.Render(Sql.DtoParamNames<ItemDto>()).Text.Should().Be("@Id, @DisplayName, @IsActive");
		syntax.Render(Sql.DtoParamNames<ItemDto>().Renamed(x => x + "_")).Text.Should().Be("@Id_, @DisplayName_, @IsActive_");
		syntax.Render(Sql.DtoParamNames<ItemDto>().Renamed(x => x + "_").Renamed(x => x + "!")).Text.Should().Be("@Id_!, @DisplayName_!, @IsActive_!");
	}

	[Test]
	public void DtoParamNamesWhereSql()
	{
		var syntax = SqlSyntax.MySql;

		syntax.Render(Sql.DtoParamNames<ItemDto>().Where(NotId)).Text.Should().Be("@DisplayName, @IsActive");
		syntax.Render(Sql.DtoParamNames<ItemDto>().Where(NotId).Where(x => x is not "DisplayName")).Text.Should().Be("@IsActive");
		syntax.Render(Sql.DtoParamNames<ItemDto>().Where(NotId).Renamed(x => x + "_")).Text.Should().Be("@DisplayName_, @IsActive_");
		syntax.Render(Sql.DtoParamNames<ItemDto>().Renamed(x => x + "_").Where(NotId)).Text.Should().Be("@Id_, @DisplayName_, @IsActive_");
		syntax.Render(Sql.DtoParamNames<ItemDto>().Where(NotId).Renamed(x => x + "_").Where(x => x is not "DisplayName_")).Text.Should().Be("@IsActive_");
		syntax.Render(Sql.DtoParamNames<ItemDto>().Renamed(x => x + "_").Where(x => x is not "DisplayName_").Renamed(x => x + "!")).Text.Should().Be("@Id_!, @IsActive_!");

		static bool NotId(string x) => x != nameof(ItemDto.Id);
	}

	[TestCase("", "")]
	[TestCase("one", "one")]
	[TestCase("one,two", "one AND two")]
	[TestCase("one,two,three", "one and two and three", true)]
	public void And(string values, string sql, bool lowercase = false)
	{
		var syntax = lowercase ? SqlSyntax.Default.WithLowercaseKeywords() : SqlSyntax.Default;
		var (text, parameters) = syntax.Render(Sql.And(values.Split([','], StringSplitOptions.RemoveEmptyEntries).Select(Sql.Raw)));
		text.Should().Be(sql);
		parameters.Count.Should().Be(0);
	}

	[TestCase("", "")]
	[TestCase("one", "one")]
	[TestCase("one,two", "one OR two")]
	[TestCase("one,two,three", "one or two or three", true)]
	public void Or(string values, string sql, bool lowercase = false)
	{
		var syntax = lowercase ? SqlSyntax.Default.WithLowercaseKeywords() : SqlSyntax.Default;
		var (text, parameters) = syntax.Render(Sql.Or(values.Split([','], StringSplitOptions.RemoveEmptyEntries).Select(Sql.Raw)));
		text.Should().Be(sql);
		parameters.Count.Should().Be(0);
	}

	[Test]
	public void AndOrAnd()
	{
		var (text, parameters) = Render(Sql.And(Sql.Raw("one"), Sql.Or(Sql.Raw("two"), Sql.And(Sql.Raw("three")))));
		text.Should().Be("one AND (two OR three)");
		parameters.Count.Should().Be(0);
	}

	[TestCase("", "")]
	[TestCase("true", "WHERE true")]
	[TestCase("true", "where true", true)]
	public void Where(string condition, string sql, bool lowercase = false)
	{
		var syntax = lowercase ? SqlSyntax.Default.WithLowercaseKeywords() : SqlSyntax.Default;
		var (text, parameters) = syntax.Render(Sql.Where(Sql.Raw(condition)));
		text.Should().Be(sql);
		parameters.Count.Should().Be(0);
	}

	[TestCase("", "")]
	[TestCase("x asc", "ORDER BY x asc")]
	[TestCase("x asc;y desc", "order by x asc, y desc", true)]
	public void OrderBy(string columns, string sql, bool lowercase = false)
	{
		var syntax = lowercase ? SqlSyntax.Default.WithLowercaseKeywords() : SqlSyntax.Default;
		var (text, parameters) = syntax.Render(Sql.OrderBy(columns.Split([';'], StringSplitOptions.RemoveEmptyEntries).Select(Sql.Raw)));
		text.Should().Be(sql);
		parameters.Count.Should().Be(0);
	}

	[TestCase("", "")]
	[TestCase("x", "GROUP BY x")]
	[TestCase("x;y", "group by x, y", true)]
	public void GroupBy(string columns, string sql, bool lowercase = false)
	{
		var syntax = lowercase ? SqlSyntax.Default.WithLowercaseKeywords() : SqlSyntax.Default;
		var (text, parameters) = syntax.Render(Sql.GroupBy(columns.Split([';'], StringSplitOptions.RemoveEmptyEntries).Select(Sql.Raw)));
		text.Should().Be(sql);
		parameters.Count.Should().Be(0);
	}

	[TestCase("", "")]
	[TestCase("x < 1", "HAVING x < 1")]
	[TestCase("x < 1", "having x < 1", true)]
	public void Having(string condition, string sql, bool lowercase = false)
	{
		var syntax = lowercase ? SqlSyntax.Default.WithLowercaseKeywords() : SqlSyntax.Default;
		var (text, parameters) = syntax.Render(Sql.Having(Sql.Raw(condition)));
		text.Should().Be(sql);
		parameters.Count.Should().Be(0);
	}

	[Test]
	public void Clauses()
	{
		var sql = Sql.Clauses(Sql.Raw("select *"), Sql.Raw("from Widgets"));
		sql.ToString().Should().Be("select *\nfrom Widgets");
	}

	private static (string Text, DbParameters Parameters) Render(Sql sql) => SqlSyntax.Default.Render(sql);

	private sealed class ItemDto
	{
		[Column("ItemId")]
		public int Id { get; set; }

		public string? DisplayName { get; set; }

		public bool IsActive { get; set; }
	}
}
