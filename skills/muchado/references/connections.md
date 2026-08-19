# Connections

To use MuchAdo, create a `DbConnector` by calling its constructor with a newly created [`IDbConnection`](https://learn.microsoft.com/en-us/dotnet/api/system.data.idbconnection), which you can either get from `DbDataSource.CreateConnection` or by creating the connection directly. Dispose the connector when you are done with it, which will automatically dispose the database connection.

```csharp
await using var connector = new DbConnector(dataSource.CreateConnection());
```

Like `IDbConnection`, `DbConnector` is not thread-safe, so you will need one instance per connection. Consider defining a method to create a connector for your database.

```csharp
private DbConnector CreateConnector() => new DbConnector(CreateConnection());
```

If you are using a database provider with [its own MuchAdo package](./databases.md), use the corresponding connector class that derives from `DbConnector`. For example, with MuchAdo.MySql, you would create a `MySqlDbConnector` with a `MySqlConnection`.

```csharp
private MySqlDbConnector CreateConnector() =>
    new MySqlDbConnector(new MySqlConnection(GetConnectionString()));
```

A `DbConnector` should be created with a new, closed `IDbConnection`. The connection will be opened automatically when a command is executed or a transaction is started, and will remain open until the connector is disposed.

To close the connection before the connector is disposed, call `CloseConnectionAsync` on the connector. This can be useful for releasing database resources during long-running work between database commands. The next command or transaction after closing the connection will automatically open the connection again.

:::info
Every asynchronous method in MuchAdo uses the `Async` suffix, accepts an optional `CancellationToken`, and has a synchronous equivalent without the `Async` suffix, e.g. `CloseConnection`. The asynchronous methods should generally be used, unless your ADO.NET provider doesn't support asynchronous I/O (e.g. [SQLite](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/async)), in which case you should use the synchronous methods.

Also note that asynchronous methods in this library return `ValueTask`, not `Task`, so be sure to follow the [relevant guidelines](https://docs.microsoft.com/dotnet/api/system.threading.tasks.valuetask-1), e.g. don't await a `ValueTask` more than once.
:::

If you want to open the connection before executing the first command, call `OpenConnectionAsync` on the connector. You can dispose the returned object to close the connection; otherwise it will be kept open until the connector is disposed.

## Connector Settings

The default `DbConnector` settings are often sufficient, especially if you are using a provider-specific package, but you can optionally pass a `DbConnectorSettings` to the `DbConnector` constructor. For efficiency, consider using a singleton for the settings rather than creating a new settings object every time you create a new connector.

The connector settings are:

* `SqlSyntax` — Controls the SQL dialect used when formatting SQL, including identifier quoting, parameter placeholders, keyword casing, and DTO column naming. See [Formatted SQL](./formatted-sql.md#sql-syntax).
* `DataMapper` — Controls how data records are mapped to .NET values. See [Data Mapping](./data-mapping.md).
* `DefaultTransactionSettings` — Supplies the default transaction settings, such as the isolation level. See [Transaction Settings](./transactions.md#transaction-settings).
* `DefaultTimeout` — Supplies the default command timeout. Use `WithTimeout` to override it for an individual command. See [Setting the Timeout](./commands.md#setting-the-timeout).
* `CacheCommands` — Caches commands by default. See [Cached Commands](./optimizations.md#cached-commands).
* `PrepareCommands` — Prepares commands by default. See [Prepared Commands](./optimizations.md#prepared-commands).
* `NoDisposeConnection` — Prevents the connector from disposing the underlying connection when the connector is disposed. An open connection is also left open.
* `CancelUnfinishedCommands` — Cancels a command when its reader is not read to the end, such as when an `Enumerate` loop exits early. See [Lazy Reading](./commands.md#lazy-reading).
* `RetryPolicy` — Supplies the policy used when opening connections and by the explicit retry methods. See [Resilience](./resilience.md).

To dispose an arbitrary object when the connector is disposed, call `AttachDisposable` on the connector after it is created.

Each provider-specific package has its own settings class derived from `DbConnectorSettings`. These classes inherit the common settings above and set provider-appropriate defaults.

### MySql

`MySqlDbConnector` uses `MySqlDbConnectorSettings`:

* `SqlSyntax` defaults to `SqlSyntax.MySql`, which uses backticks for identifiers and `?` for unnamed parameters. See [Formatted SQL](./formatted-sql.md#sql-syntax).
* `DataMapper` defaults to `MySqlDbDataMapper.Default`, which includes MySQL-specific type mappers. See [Databases](./databases.md#mysql) and [Data Mapping](./data-mapping.md#value-types).

For example, you can customize the inherited SQL syntax settings while retaining the MySQL defaults:

```csharp
private MySqlDbConnector CreateConnector() => new MySqlDbConnector(
    new MySqlConnection(GetConnectionString()), s_connectorSettings);

private static readonly MySqlDbConnectorSettings s_connectorSettings = new()
{
    SqlSyntax = SqlSyntax.MySql.WithSnakeCaseColumnNames(),
};
```

### PostgreSQL

`NpgsqlDbConnector` uses `NpgsqlDbConnectorSettings`:

* `SqlSyntax` defaults to `SqlSyntax.Postgres`, which uses double quotes for identifiers and numbered placeholders for unnamed parameters (`$1`, `$2`, etc.). See [Formatted SQL](./formatted-sql.md#sql-syntax).

### SQLite

`SqliteDbConnector` uses `SqliteDbConnectorSettings`:

* `SqlSyntax` defaults to `SqlSyntax.Sqlite`, which uses double quotes for identifiers.
* SQLite deferred transactions use the separate `SqliteDbTransactionSettings.Deferred` setting. See [Transaction Settings](./transactions.md#transaction-settings).

### SQL Server

`SqlServerDbConnector` uses `SqlServerDbConnectorSettings`:

* `SqlSyntax` defaults to `SqlSyntax.SqlServer`, which uses brackets for identifiers. See [Formatted SQL](./formatted-sql.md#sql-syntax).

## ADO.NET access

If you need to access the `IDbConnection` that is wrapped by the connector, use the `Connection` property. To automatically open the connection if it is not already open, use `GetOpenConnectionAsync` instead.

The `Transaction`, `ActiveCommand`, `ActiveBatch`, and `ActiveReader` properties expose the objects currently tracked by the connector. They return `null` if no corresponding operation is in progress.
