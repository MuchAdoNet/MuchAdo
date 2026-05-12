# Plan: Use Coverlet to Improve and Maintain Code Coverage

## Goals

* Produce repeatable coverage reports for the MuchAdo production assemblies.
* Keep coverage collection aligned with the existing NUnit, central package management, and `build.ps1` workflow.
* Start from a measured baseline instead of guessing at a target.
* Use coverage to find meaningful test gaps, then ratchet gates upward without making coverage maintenance noisy.

## Current State

* Coverage is configured through `coverlet.collector`, `coverage.runsettings`, and the `coverage` build target.
* Package versions are centralized in `Directory.Packages.props`.
* The test suite uses NUnit through five test projects under `tests`.
* Test projects target `net10.0` on every OS, plus `net462` or `net481` on Windows.
* GitHub Actions runs `restore`, `build`, `test`, `package`, and conditional `publish` through `build.ps1` on Ubuntu, Windows, and macOS.
* `build.ps1` delegates to the `Faithlife.Build` standard .NET targets, and coverage is implemented as an additional C# build target.

## Phase 1: Add Reproducible Coverage Collection

* Add `coverlet.collector` to central package management and reference it from each test project with `PrivateAssets="all"`.
* Add a shared `coverage.runsettings` file at the repository root. Configure it to:
  * emit Cobertura output for CI tooling;
  * optionally emit JSON output for later merging or troubleshooting;
  * include MuchAdo production assemblies;
  * exclude test assemblies and generated/compiler-generated code;
  * avoid broad exclusions until the first report has been reviewed.
* Prefer `coverlet.collector` as the default collection path because it works naturally with `dotnet test --collect:"XPlat Code Coverage"` and the existing NUnit/VSTest setup.
* Run the full test suite for coverage, including Docker-categorized MySQL, PostgreSQL, and SQL Server tests, so provider assemblies contribute to the baseline.
* Keep `coverlet.msbuild` as an optional second step only if the project wants MSBuild-native threshold enforcement. Do not add both modes until the baseline report proves that the collection shape is correct.

Suggested local command for the first implementation:

```powershell
.\build.ps1 coverage
```

Use `net10.0` as the primary coverage target so each test project contributes once. Continue running legacy .NET Framework targets for correctness, but do not include them in the first coverage gate because multi-targeted runs can duplicate coverage counts and make trends harder to read.

## Phase 2: Generate Human-Readable Reports

* Add a `coverage` build target that runs ReportGenerator through `dotnet dnx dotnet-reportgenerator-globaltool`.
* Generate reports from the direct Coverlet `coverage.cobertura.xml` files into `artifacts/Coverage/Report`.
* Produce at least these outputs:
  * HTML for local inspection;
  * Cobertura for services that can ingest coverage;
  * Markdown summary for CI logs or pull request comments.
* Keep `artifacts/` ignored by git if it is not already ignored.

Suggested report command:

```powershell
dotnet dnx dotnet-reportgenerator-globaltool -reports:.\artifacts\TestResults\Coverage\*\coverage.cobertura.xml -targetdir:.\artifacts\Coverage\Report -reporttypes:Html;Cobertura;MarkdownSummaryGithub
```

## Phase 3: Establish the Baseline

* Run the full `net10.0` coverage command locally and in CI.
* Record the starting line, branch, and method coverage in the pull request that introduces coverage.
* Treat branch coverage as advisory at first; line coverage is usually the least noisy initial gate.
* Inspect the lowest-covered files before setting any threshold. Classify gaps as:
  * important behavior that needs tests;
  * provider-specific behavior that requires database integration coverage;
  * framework-specific compatibility branches;
  * generated or intentionally trivial code that may merit explicit exclusion.
* Set the first line coverage gate slightly below the measured baseline so unrelated changes do not fail immediately. A good starting point is two to three percentage points below baseline.

## Phase 4: Improve Coverage Where It Reduces Risk

Prioritize tests around code that carries behavior, state, formatting, or reflection risk:

* SQL formatting and parameter handling: `Sql`, `SqlParam`, `SqlParamSource`, `SqlFormatStringHandler`, and `SqlIdentifierQuoting`.
* Command execution and command batch behavior: `DbConnectorCommand`, `DbConnectorCommandBatch`, `DbConnectorCommandBuilder`, and result-set reading.
* DTO and record mapping: `DbDataMapper`, `DbDtoInfo`, `DbDtoProperty`, `DbConnectorRecord`, and mapper implementations.
* Connection, transaction, and pooling behavior: `DbConnector`, `DbConnectorPool`, disposers, and transaction settings.
* Retry behavior: `DbRetryPolicy`, `MuchAdo.Polly`, and failure paths around transient exceptions.
* Provider wrappers and settings: MySQL, Npgsql, SQLite, and SQL Server connector settings and provider-specific type mapping.
* Analyzer behavior: add dedicated analyzer tests if normal test execution does not load the analyzer assembly in a way that Coverlet can measure.

Avoid chasing 100 percent coverage. Prefer tests that verify externally visible behavior and edge cases over tests that only execute trivial property accessors.

## Phase 5: Add CI Reporting and Gates

* Add a separate coverage step or job after the normal build/test steps. Start with one OS, preferably Ubuntu, unless provider tests require Windows-specific setup.
* Keep the existing OS matrix test job as the correctness signal.
* Upload the HTML report and Cobertura XML as GitHub Actions artifacts.
* Add a short Markdown summary to the workflow log, and optionally to pull requests after the report shape is stable.
* Once the baseline is trusted, enforce a repository-wide line coverage threshold.
* Ratchet the threshold upward only when real tests raise the baseline. Increase by small increments, such as one or two percentage points per improvement.
* Do not lower the threshold in feature work unless the pull request explains the risk and includes a follow-up issue.

If MSBuild threshold enforcement is chosen later, use a dedicated coverage command such as:

```powershell
dotnet test .\MuchAdo.slnx --configuration Release --framework net10.0 /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura /p:Threshold=70 /p:ThresholdType=line /p:ThresholdStat=total
```

Tune the threshold value from the measured baseline, not from this example.

## Phase 6: Keep Coverage Maintainable

* Review coverage reports when touching core mapping, SQL construction, transaction, pooling, or retry code.
* Add tests for bug fixes before raising thresholds so the gate reflects behavior that matters.
* Revisit exclusions quarterly or before releases; each exclusion should still be justified.
* Track branch coverage as an early warning for complex logic, even if line coverage is the enforced gate.
* Keep coverage artifacts out of source control.
* Document the local coverage command in `CONTRIBUTING.md` after the first implementation is merged.

## Definition of Done

* `coverlet.collector` is referenced consistently by all test projects.
* A shared runsettings file controls coverage inclusion and exclusion.
* Local developers can run one documented command to generate coverage.
* CI publishes a readable coverage artifact.
* The initial baseline is recorded.
* A conservative line coverage gate is enforced after the baseline is reviewed.
* Future coverage improvements ratchet the gate upward without blocking unrelated maintenance work.
