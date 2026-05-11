# Plan: Tests to Improve Coverage

## Baseline

This plan starts from the coverage report generated on 2026-05-11 by `./build.ps1 coverage`:

* Overall line coverage: 82.2%.
* Overall branch coverage: 75.4%.
* Core `MuchAdo` line coverage: 86.4%.
* Provider line coverage: MySQL 48.4%, Npgsql 52.3%, SQLite 54.8%, SQL Server 52.9%.
* The lowest-value gaps are provider connector branches, SQLite batch-reader branches, and a small set of primitive/type mappers that currently have little or no direct coverage.

The proposed tests below are ordered by expected value: cover behavior first, then use coverage gains as a side effect.

## Phase 1: Add Provider Connector Parity Tests

Provider assemblies are the clearest coverage gap, and the existing provider tests are uneven. Add parity tests across `MuchAdo.MySql.Tests`, `MuchAdo.Npgsql.Tests`, `MuchAdo.Sqlite.Tests`, and `MuchAdo.SqlServer.Tests` where each provider supports the behavior.

* Add typed connection/accessor tests for each provider:
  * Verify `Connection`, `GetOpenConnection`, and `GetOpenConnectionAsync` return the provider-specific connection type.
  * During command execution, verify `ActiveCommand`, `ActiveReader`, and `Transaction` expose provider-specific objects when available.
  * During batched execution, verify provider batch paths are active for MySQL, Npgsql, and SQLite.
* Add transaction binding tests:
  * Begin a transaction through each provider connector.
  * Execute inserts inside the transaction and verify commit and rollback behavior.
  * Include async variants for providers where async transaction APIs are overridden.
* Add command timeout tests:
  * Set a short command timeout through connector settings or command configuration.
  * Verify the timeout is applied to normal commands and to batch commands.
  * Keep these deterministic by asserting configured command state when possible, and only use slow SQL when the provider lacks observable command state.
* Add provider stored procedure/function tests:
  * Keep the existing MySQL stored procedure tests.
  * Add SQL Server input and input/output stored procedure tests analogous to the MySQL coverage.
  * Add an Npgsql function or stored procedure test that exercises `CommandType.StoredProcedure` or the closest supported provider path.

Expected payoff: raises provider assembly coverage and covers important provider-specific override branches in `MySqlDbConnector`, `NpgsqlDbConnector`, `SqliteDbConnector`, and `SqlServerDbConnector`.

## Phase 2: Expand Prepare, Cache, and Batch Shape Tests

Prepared and cached commands are high-risk because they combine SQL text, parameter identity, command shape, and provider behavior.

* Add `PrepareCacheTests` to `NpgsqlTests`, mirroring the existing MySQL, SQLite, and SQL Server tests:
  * Successful prepared cached insert with matching parameters.
  * Failure when an extra parameter is supplied.
  * Failure when a required parameter is missing.
  * Failure when the same parameters are supplied in a different order, if the provider path treats order as part of the cached command shape.
* Add batch tests that use multiple command types:
  * Text command followed by stored procedure/function command where supported.
  * Batched non-query commands followed by row-returning commands.
  * Sync and async execution paths.
* Add repeat-parameter tests per numbered/unnumbered provider:
  * MySQL unnamed placeholders reuse `?` values correctly.
  * Npgsql numbered placeholders reuse `$1` correctly.
  * SQL Server named placeholders do not accidentally collapse distinct values.
* Add typed parameter tests:
  * Use `Sql.Param(value, SqlParamType.Create(...))`, `Sql.NamedParam(name, value, type)`, and `Sql.RepeatParam(value, type)`.
  * Assert the custom `SqlParamType` is applied to the generated `DbParameter`.

Expected payoff: improves coverage for provider command building, command cache key comparison, typed SQL parameters, repeat typed parameters, and provider-specific parameter collection branches.

## Phase 3: Strengthen SQLite Batch Reader Coverage

SQLite has a custom batch implementation, so its tests should be more detailed than the provider wrappers that rely on native batch APIs.

* Add `BatchDataReader_DelegatesMetadataAndTypedAccessors`:
  * Use `QueryMultiple` over batched SQLite commands.
  * Assert `FieldCount`, `GetName`, `GetOrdinal`, `GetFieldType`, `GetDataTypeName`, indexers, `GetValue`, `GetValues`, and typed getters.
* Add `BatchDataReader_SkipsNonRowCommandsBetweenResultSets`:
  * Place inserts, updates, or DDL between selects.
  * Verify `NextResult` skips commands with no fields and still reaches later row-returning commands.
* Add `BatchDataReader_DisposedReaderThrowsForReadAndNextResult`:
  * Dispose the reader.
  * Assert `Read` and `NextResult` throw `ObjectDisposedException`.
* Add `BatchDataReader_EmptyBatchThrows`:
  * Create an empty command batch and call a row-returning operation.
  * Assert the user-facing exception is clear and stable.
* Add async batch variants:
  * Execute multiple SQLite commands with `ExecuteAsync`, `QueryMultiple`, and async result-set reads.
  * Include prepared batch execution to cover `PrepareAsync` with non-empty command lists.

Expected payoff: covers the custom `SqliteBatch`, `SqliteBatchDataReader`, and `SqliteBatchTimeout` behavior that is not exercised by normal SQLite command tests.

## Phase 4: Add a Focused Mapper Matrix

The core assembly is healthy overall, but several primitive mapper classes are at 0% or low coverage. Add a compact mapper matrix in `DbDataMapperTests` instead of many isolated one-off tests.

* Add non-null and null coverage for primitive mappers:
  * `bool`, `byte`, `char`, `DateTime`, `float`, `Guid`, `short`, and nullable variants.
  * Assert non-null values map correctly and non-nullable mappings throw on database nulls.
* Add `GetFieldValueMapper<T>` coverage:
  * Cover `DateTimeOffset`, `TimeSpan`, and unsigned numeric types where the provider can return them reliably.
  * Use a real `DbDataReader` for the success path because this mapper intentionally requires one.
  * Add a small fake `IDataRecord` test for the failure path that should throw when the record is not a `DbDataReader`.
* Add text/blob mapper branch coverage:
  * For `TextReader`, cover both the `DbDataReader.GetTextReader` path and the fallback `StringReader` path.
  * For `Stream` and `byte[]`, cover non-null, null, and fallback record paths where practical.
* Add provider-specific MySQL type mapper tests:
  * Keep the existing `MySqlDecimal` test.
  * Add `MySqlDateTime` and `MySqlGeometry` tests if the Docker MySQL image can create reliable values for those types.

Expected payoff: quickly raises low core mapper classes while also verifying conversions that users observe through `record.Get<T>()` and DTO mapping.

## Phase 5: Exercise Multi-Result Reader Facets

`DbResultSetReader` has broad API surface. Existing tests cover common `ReadSingle` flows, but not every sync/async and delegate/null-argument branch.

* Add multi-result tests for these methods:
  * `Read<T>()` and `Read<T>(map)`.
  * `ReadFirst`, `ReadFirstOrDefault`, `ReadSingle`, and `ReadSingleOrDefault`.
  * Async equivalents.
  * `Enumerate<T>()`, `Enumerate<T>(map)`, `EnumerateAsync<T>()`, and `EnumerateAsync<T>(map)`.
* Add argument validation tests:
  * Passing a null map delegate to each delegate overload throws `ArgumentNullException`.
* Add disposal tests:
  * Disposing `DbResultSetReader` releases the active reader and command or batch.
  * Async disposal follows the async disposal path.

Expected payoff: improves `DbResultSetReader`, active reader/command disposer branch coverage, and some connector state transitions.

## Phase 6: Add Analyzer Coverage Separately

The current coverage summary does not include `MuchAdo.Analyzers`. Treat analyzer coverage as a separate track so runtime database tests do not get tangled with Roslyn test harness setup.

* Add an analyzer test project or expand an existing test project with `Microsoft.CodeAnalysis.CSharp.Testing` helpers.
* Add tests for `InterpolatedCommandAnalyzer` diagnostics:
  * Interpolated SQL passed to the wrong API reports a diagnostic.
  * Correct `Sql.Format` or command builder usage produces no diagnostic.
  * Edge cases with nested interpolation and raw strings are stable.
* Update the coverage target only after analyzer tests are wired into normal test execution.

Expected payoff: brings the analyzer assembly into the coverage picture without slowing the provider integration test loop.

## Verification

After each phase, run:

```powershell
./build.ps1 coverage
```

Review `artifacts/Coverage/Report/SummaryGithub.md` after each phase. Record the line and branch deltas for the touched assemblies, not just the repository-wide aggregate.

## Suggested First PR Scope

The first test-focused PR should include:

* Npgsql `PrepareCacheTests` and provider typed accessor tests.
* SQLite batch reader metadata, disposal, and empty-batch tests.
* Core mapper matrix for the currently 0% primitive mappers.
* Typed and repeat typed parameter tests in the SQL syntax or parameter source test area.

That scope targets the biggest measured gaps while staying small enough to review without mixing in analyzer infrastructure.