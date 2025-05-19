using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using MySqlConnector;
using NUnit.Framework;

namespace MuchAdo.MySql.Tests.Examples;

[TestFixture(Explicit = true), NonParallelizable]
[SuppressMessage("ReSharper", "InterpolatedStringExpressionIsNotIFormattable", Justification = "Custom formatting.")]
internal sealed class ExampleTests
{
    [Test]
    public async Task Examples()
    {
        await using (var setupConnector = CreateConnector())
            await setupConnector.Command("drop table if exists widgets").ExecuteAsync();

        await using var connector = CreateConnector();

        await connector
            .Command("""
                create table widgets (
                    id bigint not null auto_increment primary key,
                    name text not null,
                    height real not null)
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

        var widgetsById = new Dictionary<int, Widget>();
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
    }

    private sealed record Widget(int Id, string Name, double Height);

    private static string GetConnectionString() =>
        "Server=localhost;User Id=root;Password=test;SSL Mode=none;Database=test;Ignore Prepare=false;AllowPublicKeyRetrieval=true";

    private MySqlDbConnector CreateConnector() => new MySqlDbConnector(
        new MySqlConnection(GetConnectionString()), s_connectorSettings);

    private static readonly MySqlDbConnectorSettings s_connectorSettings = new()
    {
        SqlSyntax = SqlSyntax.MySql.WithSnakeCaseColumnNames(),
    };
}
