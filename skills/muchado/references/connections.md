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

Each provider-specific package has its own settings class derived from `DbConnectorSettings`, e.g. `MySqlDbConnectorSettings`, which may have settings specific to that provider.

```csharp
private MySqlDbConnector CreateConnector() => new MySqlDbConnector(
    new MySqlConnection(GetConnectionString()), s_connectorSettings);

private static readonly MySqlDbConnectorSettings s_connectorSettings = new()
{
    SqlSyntax = SqlSyntax.MySql.WithSnakeCaseColumnNames(),
};
```

To attach an `IDbConnection` to a connector without disposing it when the connector is disposed, use the `NoDisposeConnection` connector setting. If the attached connection is open, it will be kept open even after the connector is disposed.

To dispose an arbitrary object when the connector is disposed, call `AttachDisposable` on the connector after it is created.

## ADO.NET access

If you need to access the `IDbConnection` that is wrapped by the connector, use the `Connection` property. To automatically open the connection if it is not already open, use `GetOpenConnectionAsync` instead.
