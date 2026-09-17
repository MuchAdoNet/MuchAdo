# Commands

To execute a SQL statement, call `Command` on the connector with the SQL you want to execute, chained with a call to `ExecuteAsync` to execute that SQL. `ExecuteAsync` returns the number of rows affected.

```csharp
await connector
    .Command("""
        create table widgets (
            id bigint not null auto_increment primary key,
            name text not null,
            height real)
        """)
    .ExecuteAsync();
```

## Reading records

If the SQL statement returns data records, call `QueryAsync<T>`, which maps each record to the specified type and returns an [`IReadOnlyList<T>`](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.ireadonlylist-1).

If the record has a single field, set `T` according to the type of that field value.

```csharp
var widgetNames = await connector
    .Command("select name from widgets")
    .QueryAsync<string>();
```

If the record has multiple fields, you can read them by position into the items of a tuple. The tuple names do not affect data mapping, so be sure the tuple fields are in the right order.

```csharp
var widgetTuples = await connector
    .Command("select id, name from widgets")
    .QueryAsync<(long Id, string Name)>();
```

You can also read record fields by name into the properties of a DTO (data transfer object).

```csharp
record Widget(long Id, string Name, double? Height);
...
var widgets = await connector
    .Command("select id, name, height from widgets")
    .QueryAsync<Widget>();
```

There are many other ways to map records to types. For more details, see [Data Mapping](./data-mapping.md).

The `Query` and `QueryAsync` methods read every record returned by the command, including records in later result sets. Use [result set methods](./command-batches.md#reading-result-sets) when you need to consume them separately.

### Single records

If the SQL statement always returns a single record, you can call `QuerySingleAsync<T>`, which returns an object of type `T` for that record, but throws an exception if the query returns no records or multiple records.

```csharp
var widgetCount = await connector
    .Command("select count(*) from widgets")
    .QuerySingleAsync<long>();
```

If you would rather ignore any additional records after the first, call `QueryFirstAsync<T>` instead.

If you don't want to throw an exception when there are no records, call `QuerySingleOrDefaultAsync<T>` or `QueryFirstOrDefaultAsync<T>`, which return `default(T)` when the query returns no data records.

:::tip
When you only need one record, consider adding `limit 1` to the SQL statement to prevent unwanted records from being read and ignored. To automatically cancel a command with additional records, use the same technique as [lazy reading](#lazy-reading) below.
:::

### Lazy reading

Reading all of the records at once is usually best for performance, but if you would rather read the records one at a time, use `await foreach` with `EnumerateAsync<T>`.

```csharp
var widgetsById = new Dictionary<long, Widget>();
await foreach (var widget in connector
    .Command("select id, name, height from widgets")
    .EnumerateAsync<Widget>())
{
    widgetsById[widget.Id] = widget;
}
```

:::tip
If you break out of the loop before all records have been read, the remainder of the data may still be read under the hood. It is best to avoid this situation by only querying for data that you need, but you can automatically cancel an unfinished command by setting the `CancelUnfinishedCommands` connector setting or by calling `CancelUnfinished()` on an individual command:

```csharp
await foreach (var widget in connector
    .Command("select id, name, height from widgets")
    .CancelUnfinished()
    .EnumerateAsync<Widget>())
{
    if (widget.Id == targetId)
        break;
}
```

The command setting overrides the connector setting. Use `CancelUnfinished(false)` to opt out for an individual command when the connector default is enabled.
:::

## Using parameters

The simplest way to specify command parameters is to call `CommandFormat` instead of `Command`, which uses [formatted SQL](./formatted-sql.md) to safely inject parameter values into the SQL statement.

```csharp
var widgetId = await connector
    .CommandFormat($"select id from widgets where name = {name}")
    .QuerySingleAsync<long>();
```

For more information on using parameters with MuchAdo, see [Parameters](./parameters.md).

## Setting the timeout

To set the command timeout, which overrides the default command timeout, chain a call to `WithTimeout` before executing the command.

`WithTimeout` accepts a positive [`TimeSpan`](https://learn.microsoft.com/en-us/dotnet/api/system.timespan) or `Timeout.InfiniteTimeSpan`. Positive values are rounded up to the nearest second when used to set the actual [`CommandTimeout`](https://learn.microsoft.com/en-us/dotnet/api/system.data.idbcommand.commandtimeout).

```csharp
var averageHeight = await connector
    .Command("select avg(height) from widgets")
    .WithTimeout(TimeSpan.FromSeconds(5))
    .QuerySingleAsync<double?>();
```

Most ADO.NET providers have their own mechanism for specifying the default command timeout, but you can also override it for MuchAdo with the `DefaultTimeout` connector setting.

## Stored procedures

MuchAdo also supports stored procedures. Simply call `StoredProcedure` instead of `Command` and pass the name of the stored procedure instead of a SQL query, followed by any [parameters](./parameters.md).

```csharp
await connector
    .StoredProcedure("create_widget",
        Sql.NamedParam("widget_name", name),
        Sql.NamedParam("widget_height", height))
    .ExecuteAsync();
```

For output or input/output parameters, or when a provider requires a specific parameter type or size, see [Parameter Types](./parameters.md#parameter-types).

## Cancellation

To cancel a running command, cancel the `CancellationToken` passed to the method that executed the command, or call `Cancel` on the connector.

## Executing Event

The `Executing` event is raised right before a command or command batch is executed. The handler receives a `DbConnectorExecutingEventArgs`, with the pending `DbConnectorCommandBatch` available via the `CommandBatch` property. Use the event for logging, to prepare and/or cache all commands, etc.

## ADO.NET access

ADO.NET uses [`IDbCommand`](https://learn.microsoft.com/en-us/dotnet/api/system.data.idbcommand) to represent a database command and its collection of parameters. MuchAdo creates this command object (and any parameter objects) just in time as needed when the command is executed. While a command is being executed, the `ActiveCommand` property of the connector will be set to the corresponding `IDbCommand`.

ADO.NET uses [`IDataReader`](https://learn.microsoft.com/en-us/dotnet/api/system.data.idatareader) to read any records that result from a database command. While records are being read, the `ActiveReader` property of the connector will be set to the corresponding `IDataReader`.
