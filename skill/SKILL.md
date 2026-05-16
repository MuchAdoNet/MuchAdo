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

* open and close [database connections](references/connections.md) automatically
* use a fluent API to [execute commands](references/commands.md) and read data
* read multiple result sets from [command batches](references/command-batches.md)
* leverage [transactions](references/transactions.md) simply and correctly
* [map data records](references/data-mapping.md) into simple types, tuples, and DTOs
* use [formatted SQL](references/formatted-sql.md) to build SQL statements
* specify [parameters](references/parameters.md) for commands and stored procedures
* support [resilience](references/resilience.md) by retrying transient failures
* [improve performance](references/optimizations.md) by preparing, caching, and pooling
* provide [analyzers](references/analyzers.md) to help ensure proper use of the library
