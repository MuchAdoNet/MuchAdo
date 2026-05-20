# Databases

MuchAdo works with any ADO.NET database provider. The key class in MuchAdo is `DbConnector`, which wraps a [database connection](./connections.md) and provides fluent, powerful, efficient access to the database.

If you are using a database provider with its own MuchAdo package, you should use it for optimized default settings and provider-specific features. Each provider-specific package has its own connector class that is derived from `DbConnector`.

## MySql

Use the [MuchAdo.MySql](https://www.nuget.org/packages/MuchAdo.MySql) NuGet package if you are using [MySqlConnector](https://mysqlconnector.net/), the recommended provider for [MySQL](https://www.mysql.com/). The connector class is `MySqlDbConnector`. Key enhancements:

* uses `?` for unnamed parameter placeholders
* supports provider-specific types `MySqlDateTime`, `MySqlDecimal`, and `MySqlGeometry`
* supports async database access and command batches in older .NET frameworks
* uses backticks when quoting SQL identifiers

## PostgreSQL

Use the [MuchAdo.Npgsql](https://www.nuget.org/packages/MuchAdo.Npgsql) NuGet package if you are using [Npgsql](https://www.npgsql.org/), the recommended provider for [PostgreSQL](https://www.postgresql.org/). The connector class is `NpgsqlDbConnector`. Key enhancements:

* uses `$1`, `$2`, etc. for unnamed parameter placeholders, i.e. supports [raw mode](https://www.roji.org/parameters-batching-and-sql-rewriting)
* uses [strongly-typed parameters](https://www.npgsql.org/doc/basic-usage.html#strongly-typed-parameters) to avoid boxing of value types
* supports async database access and command batches in older .NET frameworks

## SQLite

Use the [MuchAdo.Sqlite](https://www.nuget.org/packages/MuchAdo.Sqlite) NuGet package if you are using [Microsoft.Data.Sqlite](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/), the recommended provider for [SQLite](https://www.sqlite.org/). The connector class is `SqliteDbConnector`. Key enhancement:

* supports deferred transactions
* emulates command batching (not natively supported by Microsoft.Data.Sqlite as of this writing)

## SQL Server

Use the [MuchAdo.SqlServer](https://www.nuget.org/packages/MuchAdo.SqlServer) NuGet package if you are using [Microsoft.Data.SqlClient](https://learn.microsoft.com/en-us/sql/connect/ado-net/introduction-microsoft-data-sqlclient-namespace), the recommended provider for [Microsoft SQL Server](https://learn.microsoft.com/en-us/sql/connect/ado-net/introduction-microsoft-data-sqlclient-namespace). The connector class is `SqlServerDbConnector`. Key enhancement:

* uses brackets when quoting SQL identifiers

## Other databases

The standard [MuchAdo](https://www.nuget.org/packages/MuchAdo) NuGet package should work with any ADO.NET database provider, but [let us know](https://github.com/MuchAdoNet/MuchAdo/issues) if your favorite provider doesn't work or should have its own provider-specific package.
