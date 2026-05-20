# MuchAdo

[![MuchAdo](https://img.shields.io/nuget/v/MuchAdo.svg?label=MuchAdo)](https://www.nuget.org/packages/MuchAdo)
[![MuchAdo.Analyzers](https://img.shields.io/nuget/v/MuchAdo.Analyzers.svg?label=MuchAdo.Analyzers)](https://www.nuget.org/packages/MuchAdo.Analyzers)
[![MuchAdo.MySql](https://img.shields.io/nuget/v/MuchAdo.MySql.svg?label=MuchAdo.MySql)](https://www.nuget.org/packages/MuchAdo.MySql)
[![MuchAdo.Npgsql](https://img.shields.io/nuget/v/MuchAdo.Npgsql.svg?label=MuchAdo.Npgsql)](https://www.nuget.org/packages/MuchAdo.Npgsql)
[![MuchAdo.Polly](https://img.shields.io/nuget/v/MuchAdo.Polly.svg?label=MuchAdo.Polly)](https://www.nuget.org/packages/MuchAdo.Polly)
[![MuchAdo.Sqlite](https://img.shields.io/nuget/v/MuchAdo.Sqlite.svg?label=MuchAdo.Sqlite)](https://www.nuget.org/packages/MuchAdo.Sqlite)
[![MuchAdo.SqlServer](https://img.shields.io/nuget/v/MuchAdo.SqlServer.svg?label=MuchAdo.SqlServer)](https://www.nuget.org/packages/MuchAdo.SqlServer)

<!-- DO NOT EDIT: update-repo-docs convention -->

The **MuchAdo** class library provides an intuitive API for [working with relational databases](https://muchado.net/databases) like MySQL, PostgreSQL, SQLite, and Microsoft SQL Server. It is [similar to Dapper](https://muchado.net/other-libraries) and other micro ORMs for .NET.

```csharp
var shortWidgets = await connector
    .CommandFormat(
        $"select id, name from widgets where height <= {maxHeight}")
    .QueryAsync<(long Id, string Name)>(cancellationToken);
```

To use MuchAdo, add a reference to the [NuGet package](https://muchado.net/databases) that corresponds to your database. Strongly consider adding a reference to [MuchAdo.Analyzers](https://muchado.net/analyzers) as well.

## Key Features

Follow the links below for detailed information on MuchAdo features.

* [**Databases**](https://muchado.net/databases) — Work with ADO.NET providers, including provider-specific packages and connector classes for MySQL, PostgreSQL, SQLite, and Microsoft SQL Server.
* [**Connections**](https://muchado.net/connections) — Create and dispose connectors, open and close connections automatically, and configure connector settings.
* [**Commands**](https://muchado.net/commands) — Execute SQL and stored procedures, read records with query and enumeration methods, set command timeouts, cancel commands, and handle execution events.
* [**Command Batches**](https://muchado.net/command-batches) — Execute multiple SQL statements in one database call, read multiple result sets, and build batches incrementally.
* [**Transactions**](https://muchado.net/transactions) — Run manual and automatic transactions, configure transaction settings, roll back uncommitted work, and attach existing transactions.
* [**Data Mapping**](https://muchado.net/data-mapping) — Map data records to strings, value types, enums, blobs, DTOs, tuples, dynamic objects, dictionaries, custom mappers, and mapping delegates.
* [**Formatted SQL**](https://muchado.net/formatted-sql) — Build SQL from interpolated fragments and parameter values, including raw SQL, quoted names, concatenation helpers, lists, tuples, clauses, and SQL keyword helpers.
* [**Parameters**](https://muchado.net/parameters) — Inject parameters safely, expand collection parameters, use named and unnamed parameter sources, generate DTO-based parameters, create `LIKE` parameters, set parameter types, and combine parameter sources.
* [**Resilience**](https://muchado.net/resilience) — Retry opening connections, running transactions, executing commands or command batches, and wrapping arbitrary idempotent actions.
* [**Optimizations**](https://muchado.net/optimizations) — Improve performance with prepared commands, cached commands, and connector pooling.
* [**Analyzers**](https://muchado.net/analyzers) — Add analyzer warnings for potentially incorrect library usage, such as interpolated strings passed to `Command`.
* [**Other Libraries**](https://muchado.net/other-libraries) — Compare MuchAdo with Dapper across connection handling, query buffering, command batching, transaction tracking, mapping, SQL building, parameters, and optimizations.

<!-- END DO NOT EDIT -->

For more information, please check out our [comprehensive documentation](https://muchado.net/)!
