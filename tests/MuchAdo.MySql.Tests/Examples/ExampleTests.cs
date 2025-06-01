using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using MySqlConnector;
using NUnit.Framework;

namespace MuchAdo.MySql.Tests.Examples;

[TestFixture(Explicit = true), NonParallelizable]
[SuppressMessage("ReSharper", "InterpolatedStringExpressionIsNotIFormattable", Justification = "Custom formatting.")]
[SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1400:Access modifier should be declared", Justification = "Simpler for example.")]
[SuppressMessage("Style", "IDE0040:Add accessibility modifiers", Justification = "Simpler for example.")]
internal sealed class ExampleTests
{
    [Test]
    public async Task Examples()
    {
        await using (var setupConnector = CreateConnector())
        {
            await setupConnector
                .Command("drop table if exists widgets")
                .Command("drop table if exists widget_children")
                .Command("drop procedure if exists create_widget")
                .ExecuteAsync();
        }

        await using var connector = CreateConnector();

        await connector
            .Command("""
                create table widgets (
                    id bigint not null auto_increment primary key,
                    name text not null,
                    height real)
                """)
            .Command("""
                create table widget_children (
                    parent_id bigint not null,
                    child_id bigint not null)
                """)
            .ExecuteAsync();

        await connector.Command("""
            insert into widgets (name, height)
                values ('First', 6.875);
            insert into widgets (name, height)
                values ('Second', 3.1415);
            """).ExecuteAsync();

        var maxHeight = 5.0;

        var shortWidgets = await connector
            .CommandFormat(
                $"select id, name from widgets where height <= {maxHeight}")
            .QueryAsync<(long Id, string Name)>();

        shortWidgets.Should().HaveCount(1);

        var widgetNames = await connector
            .Command("select name from widgets")
            .QueryAsync<string>();

        widgetNames.Should().HaveCount(2);

        var widgetHeights = await connector
            .Command("select height from widgets")
            .QueryAsync<double?>();

        widgetHeights.Should().HaveCount(2);

        var widgetTuples = await connector
            .Command("select id, name from widgets")
            .QueryAsync<(long Id, string Name)>();

        widgetTuples.Should().HaveCount(2);

        var widgets = await connector
            .Command("select id, name, height from widgets")
            .QueryAsync<Widget>();

        widgets.Should().HaveCount(2);
        widgets.First().Height.Should().BeGreaterThan(0);

        var widgetCount = await connector
            .Command("select count(*) from widgets")
            .QuerySingleAsync<long>();

        widgetCount.Should().Be(2);

        var widgetsById = new Dictionary<long, Widget>();
        await foreach (var widget in connector
             .Command("select id, name, height from widgets")
             .EnumerateAsync<Widget>())
        {
            widgetsById[widget.Id] = widget;
        }

        widgetsById.Should().HaveCount(2);

        var name = "Second";

        var widgetIds = await connector
            .CommandFormat($"select id from widgets where name = {name}")
            .QueryAsync<long>();

        widgetIds.Should().HaveCount(1);

        var averageHeight = await connector
            .Command("select avg(height) from widgets")
            .WithTimeout(TimeSpan.FromSeconds(5))
            .QuerySingleAsync<double?>();

        averageHeight.Should().BeGreaterThan(0);

        name = "Third";
        var height = 3.3;

        var newWidgetId = connector
            .CommandFormat(
                $"insert into widgets (name, height) values ({name}, {height})")
            .Command("select last_insert_id()")
            .QuerySingle<long>();

        newWidgetId.Should().BeGreaterThan(0);

        name = "Fourth";
        height = 4.4;

        var nextWidgetId = connector
            .CommandFormat($"""
                insert into widgets (name, height) values ({name}, {height});
                select last_insert_id();
                """)
            .QuerySingle<long>();

        nextWidgetId.Should().BeGreaterThan(0);

        name = "Fifth";
        height = 5.5;

        long widgetId;
        await using (await connector.BeginTransactionAsync())
        {
            var existingWidgetId = await connector
                .CommandFormat($"select id from widgets where name = {name}")
                .QuerySingleOrDefaultAsync<long?>();
            widgetId = existingWidgetId ?? await connector
                .CommandFormat(
                    $"insert into widgets (name, height) values ({name}, {height})")
                .Command("select last_insert_id()")
                .QuerySingleAsync<long>();

            await connector.CommitTransactionAsync();
        }

        widgetId.Should().BeGreaterThan(0);

        var shortWidgetNames = new List<string>();
        var longWidgetIds = new List<long>();

        await using (var reader = await connector
            .Command("select name from widgets where height < 5")
            .Command("select id from widgets where height >= 5")
            .QueryMultipleAsync())
        {
            shortWidgetNames.AddRange(await reader.ReadAsync<string>());
            longWidgetIds.AddRange(await reader.ReadAsync<long>());
        }

        shortWidgetNames.Should().HaveCount(3);
        longWidgetIds.Should().HaveCount(2);

        await connector
            .Command("""
                CREATE PROCEDURE create_widget (
                    IN widget_name VARCHAR(255),
                    IN widget_height INT
                )
                BEGIN
                    INSERT INTO widgets (name, height) VALUES (widget_name, widget_height);
                END
                """)
            .ExecuteAsync();

        name = "Sixth";
        height = 6.6;

        await connector
            .StoredProcedure("create_widget",
                Sql.NamedParam("widget_name", name),
                Sql.NamedParam("widget_height", height))
            .ExecuteAsync();

        var widgetNameLengths = await connector
            .Command("select id, height, length(name) from widgets")
            .QueryAsync<(Widget Widget, long NameLength)>();

        widgetNameLengths.Should().HaveCount(6);

        await connector.Command("""
            insert into widget_children (parent_id, child_id)
                values ((select id from widgets where name = 'First'), (select id from widgets where name = 'Second'))
            """).ExecuteAsync();

        var lineage = await connector
            .Command("""
                select p.id, p.name, p.height, null, c.id, c.name, c.height
                from widgets p
                join widget_children wc on wc.parent_id = p.id
                join widgets c on c.id = wc.child_id
                """)
            .QueryAsync<(Widget Parent, Widget Child)>();

        lineage.Single().Parent.Name.Should().Be("First");
        lineage.Single().Child.Name.Should().Be("Second");

        var boxedHeights = await connector
            .Command("select height from widgets")
            .QueryAsync<object?>();

        boxedHeights.Should().HaveCount(6);

        var dynamicWidgets = await connector
            .Command("select name, height from widgets")
            .QueryAsync<dynamic>();
        string firstWidgetName = dynamicWidgets[0].name;

        firstWidgetName.Should().NotBeNullOrEmpty();

        var dictionaryWidgets = await connector
            .Command("select name, height from widgets")
            .QueryAsync<Dictionary<string, object>>();
        var firstWidgetHeight = (double?) dictionaryWidgets[0]["height"];

        firstWidgetHeight.Should().NotBeNull();

        var doubledHeights = await connector
            .Command("select id, name, height from widgets")
            .QueryAsync(x => x.Get<double?>(2) * 2.0);

        doubledHeights.Should().HaveCount(6);

        var halvedHeights = await connector
            .Command("select id, name, height from widgets")
            .QueryAsync(x => x.Get<double?>("height") / 2.0);

        halvedHeights.Should().HaveCount(6);

        widgetIds = await connector
            .CommandFormat($"select id from widgets order by id")
            .QueryAsync<long>();

        var widgetIdAfter = await GetNextWidgetId(connector, widgetIds[1], reverse: false);
        widgetIdAfter.Should().Be(widgetIds[2]);
        var widgetIdBefore = await GetNextWidgetId(connector, widgetIds[1], reverse: true);
        widgetIdBefore.Should().Be(widgetIds[0]);

        widgetCount = await CountWidgets(connector, maxHeight: 5.0, minHeight: null);
        widgetCount.Should().Be(3);
        widgetCount = await CountWidgets(connector, maxHeight: null, minHeight: 5.0);
        widgetCount.Should().Be(3);
        widgetCount = await CountWidgets(connector, maxHeight: 5.0, minHeight: 3.0);
        widgetCount.Should().Be(3);
        widgetCount = await CountWidgets(connector, maxHeight: null, minHeight: null);
        widgetCount.Should().Be(6);

        var widgetsFromIds = await connector
            .CommandFormat($"""
                select id, name, height from widgets
                where id in {widgetIds:set}
                """)
            .QueryAsync<Widget>();

        widgetsFromIds.Should().HaveCount(6);

        await InsertWidgets(connector, new (string, double?)[]
        {
            ("Seventh", 7.0),
            ("Eighth", 8.0),
        });

        widgetCount = await CountWidgets(connector, maxHeight: null, minHeight: null);
        widgetCount.Should().Be(8);
    }

    private async Task<long?> GetNextWidgetId(DbConnector connector, long id, bool reverse)
    {
        const string tableName = "widgets";

        var sql = Sql.Format($"""
            select id from {Sql.Name(tableName)}
            where id {Sql.Raw(reverse ? "<" : ">")} {id}
            order by id {(reverse ? Sql.Raw("desc") : Sql.Empty)}
            limit 1
            """);
        return await connector.Command(sql).QuerySingleOrDefaultAsync<long?>();
    }

    private async Task<int> CountWidgets(DbConnector connector, double? maxHeight, double? minHeight)
    {
        var ands = new List<SqlSource>();
        if (minHeight.HasValue)
            ands.Add(Sql.Format($"height >= {minHeight.Value}"));
        if (maxHeight.HasValue)
            ands.Add(Sql.Format($"height <= {maxHeight.Value}"));
        return await connector
            .CommandFormat($"select count(*) from widgets {Sql.Where(Sql.And(ands))}")
            .QuerySingleAsync<int>();
    }

    private async Task InsertWidgets(DbConnector connector, IReadOnlyList<(string Name, double? Height)> widgets)
    {
        await connector
            .CommandFormat($"""
                insert into widgets (name, height)
                values {Sql.List(widgets.Select(x =>
                    Sql.Tuple(Sql.Param(x.Name), Sql.Param(x.Height))))}
                """)
            .ExecuteAsync();
    }

    sealed record Widget(long Id, string Name, double? Height);

    private static string GetConnectionString() =>
        "Server=localhost;User Id=root;Password=test;SSL Mode=none;Database=test;Ignore Prepare=false;AllowPublicKeyRetrieval=true";

    private MySqlDbConnector CreateConnector() => new MySqlDbConnector(
        new MySqlConnection(GetConnectionString()), s_connectorSettings);

    private static readonly MySqlDbConnectorSettings s_connectorSettings = new()
    {
        SqlSyntax = SqlSyntax.MySql.WithSnakeCaseColumnNames(),
    };
}
