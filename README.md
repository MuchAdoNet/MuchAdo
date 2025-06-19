# MuchAdo

[![MuchAdo](https://img.shields.io/nuget/v/MuchAdo.svg?label=MuchAdo)](https://www.nuget.org/packages/MuchAdo)
[![MuchAdo.Analyzers](https://img.shields.io/nuget/v/MuchAdo.Analyzers.svg?label=MuchAdo.Analyzers)](https://www.nuget.org/packages/MuchAdo.Analyzers)
[![MuchAdo.MySql](https://img.shields.io/nuget/v/MuchAdo.MySql.svg?label=MuchAdo.MySql)](https://www.nuget.org/packages/MuchAdo.MySql)
[![MuchAdo.Npgsql](https://img.shields.io/nuget/v/MuchAdo.Npgsql.svg?label=MuchAdo.Npgsql)](https://www.nuget.org/packages/MuchAdo.Npgsql)
[![MuchAdo.Sqlite](https://img.shields.io/nuget/v/MuchAdo.Sqlite.svg?label=MuchAdo.Sqlite)](https://www.nuget.org/packages/MuchAdo.Sqlite)
[![MuchAdo.SqlServer](https://img.shields.io/nuget/v/MuchAdo.SqlServer.svg?label=MuchAdo.SqlServer)](https://www.nuget.org/packages/MuchAdo.SqlServer)
[![Build](https://github.com/MuchAdoNet/MuchAdo/workflows/Build/badge.svg)](https://github.com/MuchAdoNet/MuchAdo/actions?query=workflow%3ABuild)

The **MuchAdo** class library provides an intuitive API for [working with relational databases](https://muchado.net/databases) like MySQL, PostgreSQL, SQLite, and Microsoft SQL Server. It is [similar to Dapper](https://muchado.net/other-libraries) and other micro ORMs for .NET.

```csharp
var shortWidgets = await connector
    .CommandFormat(
        $"select id, name from widgets where height <= {maxHeight}")
    .QueryAsync<(long Id, string Name)>(cancellationToken);
```

To use MuchAdo, add a reference to the [NuGet package](https://muchado.net/databases) that corresponds to your database. Strongly consider adding a reference to [MuchAdo.Analyzers](https://muchado.net/analyzers) as well.

## Key Features

* open and close [database connections](https://muchado.net/connections) automatically
* use a fluent API to [execute commands](https://muchado.net/commands) and read data
* read multiple result sets from [command batches](https://muchado.net/command-batches)
* track the [current transaction](https://muchado.net/transactions) for correct command execution
* [map data records](https://muchado.net/data-mapping) into simple types, tuples, and DTOs
* use [formatted SQL](https://muchado.net/formatted-sql) to build SQL statements
* use [parameters](https://muchado.net/parameters) with commands and stored procedures
* [improve performance](https://muchado.net/optimizations) by preparing, caching, and pooling
* provide [analyzers](https://muchado.net/analyzers) to help ensure proper use of the library

For more information, please check out our [comprehensive documentation](https://muchado.net/)!
