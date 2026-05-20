---
name: muchado
description: Use the MuchAdo .NET data-access documentation when answering questions, writing code, or explaining library behavior.
---

# Introduction

The **MuchAdo** class library provides an intuitive API for [working with relational databases](references/databases.md) like MySQL, PostgreSQL, SQLite, and Microsoft SQL Server. It is [similar to Dapper](references/other-libraries.md) and other micro ORMs for .NET.

```csharp
var shortWidgets = await connector
    .CommandFormat(
        $"select id, name from widgets where height <= {maxHeight}")
    .QueryAsync<(long Id, string Name)>(cancellationToken);
```

To use MuchAdo, add a reference to the [NuGet package](references/databases.md) that corresponds to your database. Strongly consider adding a reference to [MuchAdo.Analyzers](references/analyzers.md) as well.

## Key Features

Follow the links below for detailed information on MuchAdo features.

* [**Databases**](references/databases.md) — Work with ADO.NET providers, including provider-specific packages and connector classes for MySQL, PostgreSQL, SQLite, and Microsoft SQL Server.
* [**Connections**](references/connections.md) — Create and dispose connectors, open and close connections automatically, and configure connector settings.
* [**Commands**](references/commands.md) — Execute SQL and stored procedures, read records with query and enumeration methods, set command timeouts, cancel commands, and handle execution events.
* [**Command Batches**](references/command-batches.md) — Execute multiple SQL statements in one database call, read multiple result sets, and build batches incrementally.
* [**Transactions**](references/transactions.md) — Run manual and automatic transactions, configure transaction settings, roll back uncommitted work, and attach existing transactions.
* [**Data Mapping**](references/data-mapping.md) — Map data records to strings, value types, enums, blobs, DTOs, tuples, dynamic objects, dictionaries, custom mappers, and mapping delegates.
* [**Formatted SQL**](references/formatted-sql.md) — Build SQL from interpolated fragments and parameter values, including raw SQL, quoted names, concatenation helpers, lists, tuples, clauses, and SQL keyword helpers.
* [**Parameters**](references/parameters.md) — Inject parameters safely, expand collection parameters, use named and unnamed parameter sources, generate DTO-based parameters, create `LIKE` parameters, set parameter types, and combine parameter sources.
* [**Resilience**](references/resilience.md) — Retry opening connections, running transactions, executing commands or command batches, and wrapping arbitrary idempotent actions.
* [**Optimizations**](references/optimizations.md) — Improve performance with prepared commands, cached commands, and connector pooling.
* [**Analyzers**](references/analyzers.md) — Add analyzer warnings for potentially incorrect library usage, such as interpolated strings passed to `Command`.
* [**Other Libraries**](references/other-libraries.md) — Compare MuchAdo with Dapper across connection handling, query buffering, command batching, transaction tracking, mapping, SQL building, parameters, and optimizations.
