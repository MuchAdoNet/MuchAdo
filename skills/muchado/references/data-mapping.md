# Data Mapping

MuchAdo makes it easy to read the data records produced when executing database commands. ADO.NET implements the [`IDataRecord`](https://learn.microsoft.com/en-us/dotnet/api/system.data.idatarecord) interface (and classes derived from it) to represent that data. Each data record has one or more named fields; MuchAdo supports mapping one or more data record fields to instances of various types.

## Strings

When a data record with a single field is mapped to `string`, MuchAdo calls the [`GetString`](https://learn.microsoft.com/en-us/dotnet/api/system.data.idatarecord.getstring) method on `IDataRecord`.

```csharp
var widgetNames = await connector
    .Command("select name from widgets")
    .QueryAsync<string>();
```

Be sure to use a nullable `string` (i.e. `string?`) if the field value could be null. Since `string` is a reference type, mapping a null field value to a non-nullable string will not throw an exception, but the value will be null even though the type is non-nullable.

You can also map a text field to a [`TextReader`](https://learn.microsoft.com/en-us/dotnet/api/system.io.textreader). Dispose the `TextReader` once the text is read.

:::info
If, when mapping a data record to a type, there are unused fields, e.g. when mapping a data record with two fields to a single string, MuchAdo will throw an `InvalidOperationException`. If you would rather ignore unused fields, use `WithIgnoreUnusedFields` on the `DbDataMapper` specified by your connector settings.
:::

## Value types

The following value types are mapped by calling `IDataRecord` methods like [`GetInt32`](https://learn.microsoft.com/en-us/dotnet/api/system.data.idatarecord.getint32): `bool`, `byte`, `char`, `short`, `int`, `long`, `float`, `double`, `decimal`, `Guid`, `DateTime`.

The following value types are mapped by calling the [`GetFieldValue<T>`](https://learn.microsoft.com/en-us/dotnet/api/system.data.common.dbdatareader.getfieldvalue) method on [`DbDataReader`](https://learn.microsoft.com/en-us/dotnet/api/system.data.common.dbdatareader): `sbyte`, `ushort`, `uint`, `ulong`, `DateTimeOffset`, `TimeSpan`, `DateOnly`, `TimeOnly`. Note that not all ADO.NET providers support these types.

Be sure to use a nullable type for if the field value could be null. Mapping a null field value to a non-nullable value type like `double` will result in an `InvalidOperationException`, since a `double` cannot be null; use `double?` instead.

```csharp
var widgetHeights = await connector
    .Command("select height from widgets")
    .QueryAsync<double?>();
```

## Enumerated types

For efficiency, enumerated types are mapped using their underlying numeric type, usually `int`.

Attempting to map a text field to an enumerated type will throw an exception. To allow a text field to be parsed as an enumerated type, use `WithAllowStringToEnum` on the `DbDataMapper` specified by your connector settings. Keep in mind that a `FormatException` will be thrown if the text fails to parse.

Enumerated types are value types, so be sure to use nullable types when needed.

## Blobs

A blob can be mapped to a `byte[]` or a `Stream`. Be sure to dispose of the `Stream` once it is read.

## DTOs

If the type isn't one of the types listed above or below, it is assumed to be a DTO (data transfer object) type, i.e. a type with properties that correspond to data record fields.

When a DTO type is used, a new instance of the DTO is created, and each data record field is mapped to a DTO property whose name matches the field name, ignoring case and any underscores (so `full_name` would map successfully to `FullName`, for example). Read-only properties can be set if there is a constructor with corresponding parameters.

```csharp
record Widget(long Id, string Name, double? Height);
...
var widgets = await connector
    .Command("select id, name, height from widgets")
    .QueryAsync<Widget>();
```

If a DTO property has a `Column` attribute with a non-null `Name` property (e.g. from [System.ComponentModel.DataAnnotations](https://docs.microsoft.com/en-us/dotnet/api/system.componentmodel.dataannotations.schema.columnattribute)), that name is used instead of the property name.

If all of the field values are null, a null DTO is returned, rather than a DTO instance with null property values. If this is a possibility for your query, be sure to use a nullable DTO type.

Not every property of the DTO must be used, but every data record field must have a corresponding property, unless `WithIgnoreUnusedFields` is used.

## Tuples

Use tuples to map multiple data record fields at once. Each tuple item is read from the data record in order. The data record field names are ignored, as are the tuple item names, if any.

```csharp
var widgetTuples = await connector
    .Command("select id, name from widgets")
    .QueryAsync<(long Id, string Name)>();
```

Tuples can include multi-field types like DTOs.

```csharp
var widgetNameLengths = await connector
    .Command("select id, height, length(name) from widgets")
    .QueryAsync<(Widget Widget, long NameLength)>();
```

If the tuple has two or more multi-field types, all but the last must be terminated by a null data record value whose field name is `null` (case-insensitive), which is easily accomplished by using `null` or `NULL` in the `select` statement.

```csharp
var lineage = await connector
    .Command("""
        select p.id, p.name, p.height, null, c.id, c.name, c.height
        from widgets p
        join widget_children wc on wc.parent_id = p.id
        join widgets c on c.id = wc.child_id
        """)
    .QueryAsync<(Widget Parent, Widget Child)>();
```

## object/dynamic

Data record fields can be mapped to `object` or `dynamic`. If a single field is mapped to `object` or `dynamic`, the object from [`IDataRecord.GetValue`](https://learn.microsoft.com/en-us/dotnet/api/system.data.idatarecord.getvalue) is returned directly.

```csharp
var boxedHeights = await connector
    .Command("select height from widgets")
    .QueryAsync<object?>();
```

If multiple fields are mapped to `object` or `dynamic`, an [`ExpandoObject`](https://docs.microsoft.com/dotnet/api/system.dynamic.expandoobject) is returned where each property corresponds to the name and value of a mapped field.

```csharp
var dynamicWidgets = await connector
    .Command("select name, height from widgets")
    .QueryAsync<dynamic>();
string firstWidgetName = dynamicWidgets[0].name;
```

If all of the field values are null, a null object is returned.

:::tip
To avoid confusion, use `object` when mapping a single field and `dynamic` when mapping multiple fields.
:::

## Dictionaries

Record fields can also be mapped to a dictionary with a `string` key and any type of value, in which case each field gets a key/value pair in the dictionary. The supported dictionary types are `Dictionary<string, T>`, `IDictionary<string, T>`, `IReadOnlyDictionary<string, T>`, and `IDictionary`.

```csharp
var dictionaryWidgets = await connector
    .Command("select name, height from widgets")
    .QueryAsync<Dictionary<string, object?>>();
var firstWidgetHeight = (double?) dictionaryWidgets[0]["height"];
```

If all of the field values are null, a null dictionary is returned.

## Custom mapping

To directly support types not mentioned above, you can create a custom mapping:

* Derive a class from `DbTypeMapper<T>`, overriding the `FieldCount` and `MapCore` methods.
* Derive a class from `DbTypeMapperFactory`, overriding `TryCreateTypeMapper<T>` and returning an instance of your type mapper when the type matches.
* Set the `DataMapper` connector setting to a data mapper that includes an instance of your type mapper factory.

## Mapping delegate

For more control over the mapping, the client can specify the `map` parameter, which is of type `Func<DbConnectorRecord, T>`. That delegate will be called for each data record returned by the query. Use one of the `Get<T>` methods to map one or more fields to the specified type.

```csharp
var doubledHeights = await connector
    .Command("select id, name, height from widgets")
    .QueryAsync(x => x.Get<double?>(2) * 2.0);
```

Fields can also be accessed by name, though that uses [`IDataRecord.GetOrdinal()`](https://docs.microsoft.com/dotnet/api/system.data.idatarecord.getordinal) and is thus slightly less efficient.

```csharp
var halvedHeights = await connector
    .Command("select id, name, height from widgets")
    .QueryAsync(x => x.Get<double?>("height") / 2.0);
```

There are also `Get<T>` overloads for reading multiple consecutive fields by index or name, as well as a `FieldCount` property and `GetOrdinal` and `GetName` methods.

## ADO.NET access

If you need to access the `IDataRecord` that is wrapped by `DbConnectorRecord`, use the `As<T>` method.
