# Other Libraries

## Dapper

If you are familiar with [Dapper](https://github.com/DapperLib/Dapper), you will note similarities between it and this library. So why use MuchAdo? Here are a few key differences:

* `DbConnector` [wraps the connection](./connections.md), whereas Dapper primarly provides extension methods on `IDbConnection`.
* MuchAdo makes the choice between [buffered and unbuffered queries](./commands.md) more explicit by providing separate methods. This allows `QueryAsync` to return an `IReadOnlyList<T>` instead of an `IEnumerable<T>`.
* MuchAdo supports [command batching](./command-batches.md) with the new `DbBatch` class from ADO.NET.
* With Dapper, you must remember to set the `transaction` parameter when there is an active transaction. Since MuchAdo [tracks the current transaction](./transactions.md), it attaches it to database commands automatically.
* The [multi-mapping support](./data-mapping.md) of MuchAdo is simpler and more flexible than the `map` and `splitOn` parameters of Dapper.
* Building SQL statements with [formatted SQL](./formatted-sql.md) is more natural than with SqlBuilder.
* MuchAdo has more natural mechanisms for [specifying parameters](./parameters.md) than using named parameters with an anonymous object. It also uses the most efficient parameter placeholders for each supported database.
* MuchAdo has more explicit syntax for expanding [collection parameters](./parameters.md#collection-parameters) for `IN` clauses. MuchAdo throws an exception when an empty collection is used, since the desired behavior in that scenario is not clear, and Dapper's strategy of using `(SELECT @p WHERE 1 = 0)` doesn't work with all databases, isn't necessarily what the caller would want, and doesn't always play well with table indexes.
* MuchAdo supports [optimizations](./optimizations.md) like prepared commands and opt-in caching.
