# MuchAdo

[![Build](https://github.com/MuchAdoNet/MuchAdo/workflows/Build/badge.svg)](https://github.com/MuchAdoNet/MuchAdo/actions?query=workflow%3ABuild)

The **MuchAdo** class library provides an intuitive API for [working with relational databases](https://muchado.net/databases) like MySQL, PostgreSQL, SQLite, and Microsoft SQL Server. It is similar to Dapper and other micro ORMs for .NET.

```csharp
var widgets = await connector
    .CommandFormat(
        $"select id, name from widgets where height <= {maxHeight}")
    .QueryAsync<(long Id, string Name)>();
```

## Key Features

* generate SQL and parameters optimized for each [database provider](https://muchado.net/databases.md)
* open and close [database connections](https://muchado.net/connections.md) automatically
* use formatted strings to safely inject parameters and build complex SQL statements
* map SELECT statements into simple types, tuples, and DTOs
* track the [current transaction](https://muchado.net/transactions.md) for correct command execution
* expand collections for IN clauses and bulk INSERT
* read database records all at once or one at a time
* call synchronous methods or asynchronous methods with cancellation
* read multiple result sets from multi-statement commands or batches
* execute stored procedures with parameters
* prepare and/or cache database commands for better performance

For more information, please check out our [comprehensive documentation](https://muchado.net/)!
