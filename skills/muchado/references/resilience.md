# Resilience

MuchAdo has features to help you recover from deadlocks and other transient errors.

## Retry connections

To automatically retry opening a database connection when it throws a transient exception, set the `RetryPolicy` connector setting.

You can derive a class from the abstract `DbRetryPolicy` class, but it is simpler to add a reference to the [MuchAdo.Polly](https://www.nuget.org/packages/MuchAdo.Polly) NuGet package and use `PollyDbRetryPolicy.Create` to create an instance from a Polly resilience pipeline.

```csharp
var connector = new DbConnector(CreateConnection(),
    new DbConnectorSettings
    {
        RetryPolicy = PollyDbRetryPolicy.Create(resiliencePipeline),
    });
```

## Retry transactions

To use the retry policy to retry an auto-commit transaction, call `RetryInTransactionAsync` instead of `ExecuteInTransactionAsync`.

```csharp
await connector.RetryInTransactionAsync(async () =>
{
    await connector
        .Command("update widgets set height = height + 1")
        .ExecuteAsync();
});
```

## Retry commands

To use the retry policy to retry a command or command batch, chain a call to `Retry` before executing the command. It is up to you to ensure that a multi-command batch is idempotent, since it may be called more than once, per the retry policy. You can wrap it in a transaction if necessary, e.g. by chaining a call to `InTransaction`. Use `RetryInTransaction` as shorthand for calling both `Retry` and `InTransaction`.

```csharp
await connector
    .Command("update widgets set height = height + 1")
    .RetryInTransaction()
    .ExecuteAsync();
```

:::warning
`Retry`/`RetryInTransaction` cannot be combined with record-by-record enumeration (`Enumerate`/`EnumerateAsync`). Use the list-returning query methods instead when retries are needed. Likewise, `QueryMultiple`/`QueryMultipleAsync` without the mapping delegate cannot be paired with these retry helpers; use the delegate overload or control the retry/transaction flow yourself.
:::

## Retry any action

To use the retry policy with any action, call `RetryAsync`. It is up to you to ensure that the called code is idempotent, since it may be called more than once, per the retry policy.
