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
    public async Task Introduction()
    {
        await using (var setupConnector = CreateConnector())
            await setupConnector.Command("drop table if exists widgets").ExecuteAsync();

        await using var connector = CreateConnector();

        await connector.Command("""
            create table widgets (
                id int not null auto_increment primary key,
                name text not null,
                height real not null);
            """).ExecuteAsync();

        await connector.Command("""
            insert into widgets (name, height)
                values ('First', 6.875);
            insert into widgets (name, height)
                values ('Second', 3.1415);
            """).ExecuteAsync();

        var maxHeight = 5.0;

        var widgets = await connector
            .CommandFormat(
                $"select id, name from widgets where height <= {maxHeight}")
            .QueryAsync<(long Id, string Name)>();

        widgets.Should().HaveCount(1);
    }

    [Test]
    public async Task Transactions()
    {
        await using (var setupConnector = CreateConnector())
            await setupConnector.Command("drop table if exists widgets").ExecuteAsync();

        await using var connector = CreateConnector();

        await using (await connector.BeginTransactionAsync())
        {
            await connector.Command("""
                create table widgets (
                    id int not null auto_increment primary key,
                    name text not null,
                    height real not null);
                """).ExecuteAsync();

            await connector.Command("""
                insert into widgets (name, height)
                    values ('First', 6.875);
                insert into widgets (name, height)
                    values ('Second', 3.1415);
                """).ExecuteAsync();

            await connector.CommitTransactionAsync();
        }
    }

    private static string GetConnectionString() =>
        "Server=localhost;User Id=root;Password=test;SSL Mode=none;Database=test;Ignore Prepare=false;AllowPublicKeyRetrieval=true";

    private MySqlDbConnector CreateConnector() => new MySqlDbConnector(
        new MySqlConnection(GetConnectionString()), s_connectorSettings);

    private static readonly MySqlDbConnectorSettings s_connectorSettings = new()
    {
        SqlSyntax = SqlSyntax.MySql.WithSnakeCaseColumnNames(),
    };
}
