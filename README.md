# MuchAdo

A fluent, powerful, efficient library for querying ADO.NET databases.

[![Build](https://github.com/MuchAdoNet/MuchAdo/workflows/Build/badge.svg)](https://github.com/MuchAdoNet/MuchAdo/actions?query=workflow%3ABuild)

## Quick Start

The **MuchAdo** class library provides an intuitive API for working with ADO.NET providers like those for [MySQL](https://mysqlconnector.net/), [PostgreSQL](https://www.npgsql.org/), [Microsoft SQL Server](https://learn.microsoft.com/en-us/sql/connect/ado-net/introduction-microsoft-data-sqlclient-namespace), and [SQLite](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/). It is similar to [Dapper](https://github.com/DapperLib/Dapper) and other micro ORMs for .NET.

To use this library, add a reference to the MuchAdo NuGet package that corresponds to your ADO.NET provider. Then create a connection to your database and use it to construct a MuchAdo connector.

|  ADO.NET Provider | MuchAdo Package | MuchAdo Connector |
| --- | --- | --- |
| [MySqlConnector](https://mysqlconnector.net/) | [![MuchAdo.MySql](https://img.shields.io/nuget/v/MuchAdo.MySql.svg?label=MuchAdo.MySql)](https://www.nuget.org/packages/MuchAdo.MySql) | MySqlDbConnector |
| [Npgsql](https://www.npgsql.org/) | [![MuchAdo.Npgsql](https://img.shields.io/nuget/v/MuchAdo.Npgsql.svg?label=MuchAdo.Npgsql)](https://www.nuget.org/packages/MuchAdo.Npgsql) | NpgsqlDbConnector |
| [Microsoft.Data.Sqlite](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/) | [![MuchAdo.Sqlite](https://img.shields.io/nuget/v/MuchAdo.Sqlite.svg?label=MuchAdo.Sqlite)](https://www.nuget.org/packages/MuchAdo.Sqlite) | SqliteDbConnector |
| [Microsoft.Data.SqlClient](https://learn.microsoft.com/en-us/sql/connect/ado-net/introduction-microsoft-data-sqlclient-namespace) | [![MuchAdo](https://img.shields.io/nuget/v/MuchAdo.svg?label=MuchAdo)](https://www.nuget.org/packages/MuchAdo) | DbConnector |
| any ADO.NET provider | [![MuchAdo](https://img.shields.io/nuget/v/MuchAdo.svg?label=MuchAdo)](https://www.nuget.org/packages/MuchAdo) | DbConnector |

Here's a simple code sample that opens an in-memory SQLite database, creates a table, inserts a few rows within a transaction, and runs a couple of queries. The example uses synchronous methods, which are appropriate for SQLite, but every synchronous method has an equivalent asynchronous method, e.g. `ExecuteAsync`. There's no risk of SQL injection attacks with the interpolated strings, which use **formatted SQL**. [**Try it!**](https://dotnetfiddle.net/SZ5VHq)

```csharp
using var connector = new SqliteDbConnector(
    new SqliteConnection("Data Source=:memory:"));

connector
    .Command("""
        create table widgets (
            id integer primary key autoincrement,
            name text not null,
            height real not null)
        """)
    .Execute();

var widgets = new[]
{
    new Widget("First", 6.875),
    new Widget("Second", 1.414),
    new Widget("Third", 3.1415),
};

using (connector.BeginTransaction())
{
    foreach (var widget in widgets)
    {
        connector
            .CommandFormat($"""
                insert into widgets (name, height)
                values ({widget.Name}, {widget.Height})
                """)
            .Execute();
    }

    connector.CommitTransaction();
}

var maxHeight = 5.0;
foreach (var widget in connector
    .CommandFormat($"""
        select name, height
        from widgets
        where height <= {maxHeight}
        """)
    .Query<Widget>())
{
    Console.WriteLine($"short: {widget}");
}

var (min, max) = connector
    .Command("select min(height), max(height) from widgets")
    .QuerySingle<(double, double)>();
Console.WriteLine($"min height {min}, max height {max}");
```

## Key Features

With MuchAdo, you can easily:

* open and close database connections automatically
* track the current transaction for correct command execution
* use formatted strings to safely inject parameters and build complex SQL statements
* map SELECT statements into simple types, tuples, and DTOs
* expand collections for IN clauses and bulk INSERT
* read database records all at once or one at a time
* call synchronous methods or asynchronous methods with cancellation
* read multiple result sets from multi-statement commands or batches
* execute stored procedures with parameters
* prepare and/or cache database commands for better performance
* generate SQL and parameters optimized for each database provider
