# Parameters

The simplest way to specify command parameters is to use [formatted SQL](./formatted-sql.md) to inject parameter values into the SQL statement.

```csharp
var widgetId = await connector
    .CommandFormat($"select id from widgets where name = {name}")
    .QuerySingleAsync<long>();
```

This may look like a potential SQL injection vulnerability, but it is not, since the injected value is replaced with a parameter placeholder in the SQL statement, and the value itself is passed to the command via matching parameter. The exact syntax of the parameter placeholder depends on the database provider; by default, it uses arbitrarily named parameters like `@ado1` and `@ado2`, but some providers use `$1` and `$2` or even just `?`.

:::warning
Using string interpolation with `Command` instead of `CommandFormat` is still a potential security risk, so be sure to use `CommandFormat` when you want safe parameter injection.

```csharp
// don't do this
var widgetId = await connector
    .Command($"select id from widgets where name = {name}")
    .QuerySingleAsync<long>();
```

Use [MuchAdo.Analyzers](./analyzers.md) to generate a compiler warning when `Command` is used with an interpolated string.
:::

## Collection Parameters

Using formatted SQL with a collection will create one parameter whose value is the collection. If you want to expand a non-empty collection into a parenthesized set of parameters for use with the `IN` keyword, use the `set` format specifier.

```csharp
var widgetsFromIds = await connector
    .CommandFormat($"""
        select id, name, height from widgets
        where id in {widgetIds:set}
        """)
    .QueryAsync<Widget>();
```

:::warning
The `set` format specifier throws an exception if the collection is empty when the command is executed. Be sure to check for an empty collection and bypass the command or add a conditional to generate valid SQL for that case.
:::

## Parameter Sources

When the simple techniques above aren't sufficient, you can use a parameter source, which is derived from `SqlParamSource` and represents an ordered collection of parameters, named or unnamed.

A parameter source can be injected into formatted SQL or included as command arguments. When injected into formatted SQL, a parameter source generates placeholders for parameters with the corresponding values, comma-separated in the SQL if there are more than one. See below for examples.

### Unnamed Parameters

`Sql.Param` creates a parameter source containing a single unnamed parameter with the specified value.

```csharp
widgetId = await connector
    .Command("select id from widgets where name = ?", Sql.Param(name))
    .QuerySingleAsync<long>();
```

:::note
The returned object is of type `SqlParam<T>`, which has a `Value` property that can be set, if you want to reuse the object for multiple commands to reduce allocation.
:::

`Sql.RepeatParam` creates an unnamed parameter designed to be used more than once in the formatted SQL of a single command. For databases that use named or numbered placeholders, the same placeholder is used each time the same object from `Sql.RepeatParam` is used, which could be more efficient.

```csharp
var heightParam = Sql.RepeatParam(height);
await connector
    .CommandFormat($"""
        insert into widgets (name, height)
        values ({name}, {heightParam})
        on duplicate key update height = {heightParam}
        """)
    .ExecuteAsync();
```

`Sql.Params` generates unnamed parameters from a collection. The collection is not copied; parameters are generated from the items when the command is executed. The `set` format specifier documented above is normally simpler, but this is equivalent to the example above:

```csharp
widgetsFromIds = await connector
    .CommandFormat($"""
        select id, name, height from widgets
        where id in ({Sql.Params(widgetIds)})
        """)
    .QueryAsync<Widget>();
```

:::warning
If the collection is empty, empty SQL will be produced, which could result in invalid SQL. As with the `set` format specifier, you should check for an empty collection and bypass the command or add a conditional to generate valid SQL for that case.
:::

### Named Parameters

Unnamed parameters are simplest, but you can also create named parameters. Be sure to avoid specifying two different parameters with the same name in the same command.

`Sql.NamedParam` creates a parameter with the specified name and value. When injected into formated SQL, named parameters use the corresponding named placeholder, e.g. `@height`.

```csharp
widgetId = await connector
    .CommandFormat($"""
        select id from widgets
        where name = {Sql.NamedParam("name", name)}
        """)
    .QuerySingleAsync<long>();
```

Alternatively:

```csharp
widgetId = await connector
    .Command("select id from widgets where name = @name",
        Sql.NamedParam("name", name))
    .QuerySingleAsync<long>();
```

Named parameters can also be useful when calling stored procedures.

```csharp
await connector
    .StoredProcedure("create_widget",
        Sql.NamedParam("widget_name", name),
        Sql.NamedParam("widget_height", height))
    .ExecuteAsync();
```

`Sql.NamedParams` creates multiple parameters with the specified names and values, either from a collection of tuple pairs where the first value is the parameter name, or from a collection of key/value pairs where the key is the parameter name. As with `Sql.Params`, you should usually avoid empty collections.

```csharp
var namedParams = Sql.NamedParams(widgetIds.Select((x, i) => ($"id{i}", x)));
widgetsFromIds = await connector
    .CommandFormat($"""
        select id, name, height from widgets
        where id in ({namedParams})
        """)
    .QueryAsync<Widget>();
```

### DTOs

`Sql.DtoColumnNames` is useful when selecting fields to map to a DTO (data transfer object).

```csharp
widgets = await connector
    .CommandFormat($"select {Sql.DtoColumnNames<Widget>()} from widgets")
    .QueryAsync<Widget>();
```

If a DTO property has a `Column` attribute with a non-null `Name` property (e.g. from [System.ComponentModel.DataAnnotations](https://docs.microsoft.com/en-us/dotnet/api/system.componentmodel.dataannotations.schema.columnattribute)), that name is used instead of the property name. If you want to generate `snake_case` column names without having to add `Column` attributes everywhere, use `WithSnakeCaseColumnNames` on the `SqlSyntax` connector setting.

Chain `Sql.DtoColumnNames` with a call to `From` to include a table name or alias, e.g. `p.height`.

```csharp
lineage = await connector
    .CommandFormat($"""
        select {Sql.DtoColumnNames<Widget>().From("p")}, null,
            {Sql.DtoColumnNames<Widget>().From("c")}
        from widgets p
        join widget_children wc on wc.parent_id = p.id
        join widgets c on c.id = wc.child_id
        """)
    .QueryAsync<(Widget Parent, Widget Child)>();
```

`Sql.DtoParams` generates unnamed parameters for the value of each property of a DTO. It can be used with `Sql.DtoColumnNames` when inserting database records. With both methods, you can use `Where` to filter properties by name.

```csharp
await connector
    .CommandFormat($"""
        insert into widgets
            ({Sql.DtoColumnNames(newWidget)
                .Where(x => x != nameof(Widget.Id))})
        values
            ({Sql.DtoParams(newWidget)
                .Where(x => x != nameof(Widget.Id))})
        """)
    .ExecuteAsync();
```

If you prefer named parameters, you can use `Sql.DtoNamedParams`.

```csharp
widgetIds = await connector
    .Command("""
        select id from widgets
        where height between @minHeight and @maxHeight
        """, Sql.DtoNamedParams(new { minHeight, maxHeight }))
    .QueryAsync<long>();
```

If you want to generate named parameter placeholders for a DTO without the values, use `Sql.DtoParamNames`.

To add a prefix or suffix to help ensure that the parameter name is unique, chain a call to `Renamed`.

### LIKE Parameters

Use `Sql.LikeParamStartsWith` to create an unnamed string parameter with the specified prefix followed by a `%`, where the prefix is escaped as necessary.

```csharp
var widgetsWithNamePrefix = await connector
    .CommandFormat($"""
        select id, name, height from widgets
        where name like {Sql.LikeParamStartsWith(prefix)}
        """)
    .QueryAsync<Widget>();
```

Use `Sql.LikeParamEndsWith`, `Sql.LikeParamContains`, or `Sql.LikeParam` for other `LIKE` patterns.

### Parameter Types

MuchAdo creates ADO.NET parameter objects just in time when the command is executed. If you need to set the `DbType` or other properties on the `IDataParameter`, you can use `SqlParamType` or create the parameter object yourself.

`Sql.Param` has an overload that takes a `SqlParamType`, which can be used to set whatever parameter properties you need to set.

Alternatively, if you create an `IDataParameter` and pass it as a parameter value, MuchAdo will use it as-is rather than create a new parameter for it. In fact, an `IDataParameter` will be implicitly converted to a `SqlParamSource` as necessary.

### Combine Sources

If you need to combine multiple parameter sources into a single parameter source, use `Sql.Combine`.
