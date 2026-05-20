# Formatted SQL

Formatted SQL uses string interpolation to build SQL statements from SQL fragments and parameter values.

Formatted SQL is most commonly used when calling the `CommandFormat` method on a connector, passing an interpolated string with parameter values. `CommandFormat` is actually a convenience method that is equivalent to calling `Sql.Format` and passing the returned `SqlSource` to the `Command` method.

```csharp
SqlSource sql = Sql.Format($"select id from widgets where name = {name}");
long widgetId = await connector.Command(sql).QuerySingleAsync<long>();
```

A `SqlSource` represents a SQL fragment and any attached parameter values. The `Sql` static class contains many methods that create SqlSource objects.

## SQL Text

`Sql.Format` is the most useful method for building SQL fragments. An unnamed parameter is substituted for each interpolated expression, unless the expression is a `SqlSource`, in which case the corresponding SQL fragment is substituted.

`Sql.Raw` creates a SQL fragment from a raw SQL string.

`Sql.Empty` contains an empty SQL fragment, equivalent to `Sql.Raw("")`.

`Sql.Name` surrounds the specified string with delimiters that prevent it from being interpreted as a SQL keyword. ANSI SQL uses double quotes for this, but some databases have their own syntax, so be sure to use the right MuchAdo package or `SqlSyntax`.

```csharp
var sql = Sql.Format($"""
    select id from {Sql.Name(tableName)}
    where id {Sql.Raw(reverse ? "<" : ">")} {id}
    order by id {(reverse ? Sql.Raw("desc") : Sql.Empty)}
    limit 1
    """);
return await connector.Command(sql).QuerySingleOrDefaultAsync<long?>();
```

## SQL Building

`SqlSource` instances can be concatenated with the `+` operator or with `Sql.Concat`.

`Sql.Intersperse` works like `string.Join`; it intersperses SQL fragments with the specified raw SQL separator. Empty fragments are ignored, rather than doubling up the separator.

`Sql.Clauses` intersperses SQL fragments with newlines, equivalent to `Sql.Intersperse("\n", ...})"`.

`Sql.List` intersperses SQL fragments with commas, equivalent to `Sql.Intersperse(", ", ...})"`.

`Sql.Tuple` is shorthand for a comma-separated list surrounded by parentheses, equivalent to `Sql.Format($"({Sql.List(...)})"`.

`Sql.Set` is like `Sql.Tuple`, but it throws an exception when the list is empty, since `in ()` is not valid SQL.

```csharp
await connector
    .CommandFormat($"""
        insert into widgets (name, height)
        values {Sql.List(widgets.Select(x =>
            Sql.Tuple(Sql.Param(x.Name), Sql.Param(x.Height))))}
        """)
    .ExecuteAsync();
```

## SQL Keywords

There are a few methods that generate SQL keywords. The advantage of these methods over typing the keyword directly into formatted SQL is that the keyword is omitted if the arguments are missing or all empty.

`Sql.And` and `Sql.Or` intersperse non-empty arguments with `AND` or `OR` keywords. Each argument is surrounded with parentheses. If there is only one non-empty argument, it is used as-is.

`Sql.Where` and `Sql.Having` generate a `WHERE` or `HAVING` keyword followed by the arguments, interspersed with `AND` keywords, as above.

`Sql.OrderBy` and `Sql.GroupBy` generate `ORDER BY` or `GROUP BY` keywords followed by the arguments, interspersed with commas.

```csharp
var conditions = new List<SqlSource>();
if (minHeight.HasValue)
    conditions.Add(Sql.Format($"height >= {minHeight.Value}"));
if (maxHeight.HasValue)
    conditions.Add(Sql.Format($"height <= {maxHeight.Value}"));
return await connector
    .CommandFormat($"select count(*) from widgets {Sql.Where(conditions)}")
    .QuerySingleAsync<int>();
```

:::tip
It doesn't matter to the database, but if you want MuchAdo to generate lowercase SQL keywords, use `WithLowercaseKeywords` on your `SqlSyntax` connector setting.
:::
