# Transactions

To start a database transaction, call `BeginTransactionAsync` on the connector. MuchAdo calls the corresponding method on the ADO.NET connection to get an [`IDbTransaction`](https://learn.microsoft.com/en-us/dotnet/api/system.data.idbtransaction), which it stores with the connector.

To commit the transaction, call `CommitTransactionAsync` on the connector, which commits and then disposes the stored transaction.

```csharp
await using (await connector.BeginTransactionAsync())
{
    var existingWidgetId = await connector
        .CommandFormat($"select id from widgets where name = {name}")
        .QuerySingleOrDefaultAsync<long?>();
    widgetId = existingWidgetId ?? await connector
        .CommandFormat(
            $"insert into widgets (name, height) values ({name}, {height})")
        .Command("select last_insert_id()")
        .QuerySingleAsync<long>();

    await connector.CommitTransactionAsync();
}
```

When the object returned from `BeginTransactionAsync` is disposed, the transaction is disposed, which rolls back the transaction if it has not been committed, e.g. if an exception is thrown.

:::info
ADO.NET requres that the [`Transaction`](https://learn.microsoft.com/en-us/dotnet/api/system.data.idbcommand.transaction) property of [`IDbCommand`](https://docs.microsoft.com/dotnet/api/system.data.idbcommand) be set to the current transaction. MuchAdo takes care of that automatically when creating and executing commands.
:::

You can explicitly roll back the current transaction with `RollbackTransactionAsync`, but that's not typically necessary, since an uncommitted transaction will be rolled back when it is disposed.

## Auto-commit transactions

To automatically commit the database transaction, you can use `ExecuteInTransactionAsync`:

```csharp
widgetId = await connector.ExecuteInTransactionAsync(async () =>
{
    var existingWidgetId = await connector
        .CommandFormat($"select id from widgets where name = {name}")
        .QuerySingleOrDefaultAsync<long?>();
    return existingWidgetId ?? await connector
        .CommandFormat(
            $"insert into widgets (name, height) values ({name}, {height})")
        .Command("select last_insert_id()")
        .QuerySingleAsync<long>();
});
```

To wrap an auto-commit transaction around a command batch, chain a call to `InTransaction` before executing the command.

:::warning
Record-by-record enumeration (`Enumerate`/`EnumerateAsync`) cannot be used together with `InTransaction`. If you need automatic transactions via `InTransaction`, use the list-returning methods (`Query`/`QueryAsync`) instead. Similarly, `QueryMultiple`/`QueryMultipleAsync` without the mapping delegate cannot be paired with `InTransaction`; use the delegate overload or manage the transaction manually.
:::

## Transaction settings

The transaction methods have overloads that accept a `DbTransactionSettings`, which is typically used to specify the transaction isolation level. Feel free to use an [`IsolationLevel`](https://learn.microsoft.com/en-us/dotnet/api/system.data.isolationlevel) directly; it will be implicitly converted to `DbTransactionSettings`.

The default isolation level is provider-specific, but you can override it with the `DefaultTransactionSettings` connector setting.

:::tip
If you are using MuchAdo.Sqlite, you can use `SqliteDbTransactionSettings` to create [deferred transactions](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/transactions#deferred-transactions).
:::

## ADO.NET access

You can attach an existing `IDbTransaction` to the connector by calling `AttachTransaction`. Call it only when the connection is already open, and dispose the returned object when you are done to clear the attached transaction. Use the `noDispose` parameter when the transaction is owned elsewhere to prevent MuchAdo from disposing it.

If you need to access the `IDbTransaction` that is stored by the connector, use the `Transaction` property. If there is no active transaction, the property will be null.

:::tip
You can use the `Transaction` property to [create savepoints](https://learn.microsoft.com/en-us/dotnet/api/system.data.common.dbtransaction.save). If you would like MuchAdo to support savepoints directly, please [create an issue](https://github.com/MuchAdoNet/MuchAdo/issues)!
:::
