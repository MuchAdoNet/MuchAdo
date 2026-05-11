# Enable Docker-Backed Tests in CI

## Current State

- The provider-specific database test projects are `tests/MuchAdo.MySql.Tests`, `tests/MuchAdo.Npgsql.Tests`, and `tests/MuchAdo.SqlServer.Tests`.
- Their fixtures are marked `Explicit = true`, so the default `build.ps1 test` path discovers them but does not execute them.
- The tests use hard-coded localhost connection strings that match `docker/docker-compose.yml`:
  - MySQL: `localhost:3306`, root password `test`, database `test`.
  - PostgreSQL: `localhost:5432`, user `root`, password `test`, database `test`.
  - SQL Server: `localhost:1433`, user `sa`, password `P@ssw0rd`, database `test`.
- `docker/docker-compose.yml` defines the three database containers plus a `setup` container, but the setup script depends on generated container names such as `muchado-mssql-1` and uses a fixed sleep before creating databases.
- `.github/workflows/build.yaml` runs the normal restore/build/test/package matrix on Ubuntu, Windows, and macOS. It never starts Docker services or opts into explicit NUnit tests.

## Goals

- Keep the normal unit-test build fast and Docker-free.
- Add a repeatable command that starts the database containers, initializes the `test` databases, runs the provider integration tests, and tears containers down reliably.
- Run the Docker-backed tests in GitHub Actions on `ubuntu-latest`, where Docker Compose support is already available on hosted runners.
- Preserve the existing OS matrix for restore/build/unit/package coverage.
- Make failures easy to diagnose by publishing test results and, when container startup fails, Docker logs.

## Plan

### 1. Classify Integration Tests Explicitly

- Replace fixture-level `Explicit = true` with a consistent integration-test marker, for example `[Category("Docker")]`, on the provider fixtures or assemblies.
- Keep the default `test` target from running Docker tests by adding a default filter such as `TestCategory!=Docker` to the standard unit-test path.
- Add a dedicated integration-test path that runs only `TestCategory=Docker` for:
  - `tests/MuchAdo.MySql.Tests/MuchAdo.MySql.Tests.csproj`
  - `tests/MuchAdo.Npgsql.Tests/MuchAdo.Npgsql.Tests.csproj`
  - `tests/MuchAdo.SqlServer.Tests/MuchAdo.SqlServer.Tests.csproj`
- Verify whether NUnit explicit tests run when selected by `dotnet test --filter`; if they do not run reliably, remove `Explicit` once the default unit-test filter is in place.

### 2. Harden Docker Compose Setup

- Update `docker/docker-compose.yml` so each service exposes a health check instead of relying only on open TCP ports.
- Remove the fixed `sleep 10` from `docker/setup/setup.sh`.
- Make database initialization independent of generated container names:
  - Prefer built-in initialization where available: set `MYSQL_DATABASE=test` for MySQL and `POSTGRES_DB=test` for PostgreSQL.
  - For SQL Server, run `sqlcmd` through `docker compose exec -T mssql ...` or use a setup container that connects to the `mssql` service name directly.
- Add idempotent database creation so rerunning setup after a partial failure does not fail because the `test` database already exists.
- Pin or refresh database images deliberately. SQL Server currently uses `mcr.microsoft.com/mssql/server:2017-CU17-ubuntu`; if compatibility with supported GitHub-hosted Linux runners becomes an issue, move to a newer supported SQL Server image as part of the same change.

### 3. Add a Build Target for Docker Tests

- Extend `tools/Build/Build.cs` with a target such as `test-docker` or `integration-test`.
- The target should:
  - Depend on `build` unless `--skip build` is supplied.
  - Run `docker compose -f docker/docker-compose.yml up -d --build`.
  - Wait for service health or run the setup step that blocks until databases are ready.
  - Execute the three provider test projects with `dotnet test --no-build --configuration <configuration> --filter TestCategory=Docker`.
  - Emit TRX results under `release/TestResults` using stable file names per provider.
  - Always run `docker compose -f docker/docker-compose.yml down -v` in a `finally` path.
- If extending `Faithlife.Build` is awkward, add a checked-in PowerShell helper such as `tools/Test-Docker.ps1` and have both the build target and CI call that helper. The build target should remain the public entry point.

### 4. Update GitHub Actions

- Keep the existing `build` job matrix unchanged for unit tests and packages.
- Add a separate `docker-tests` job:
  - `runs-on: ubuntu-latest`
  - uses the same checkout and `.NET` setup steps
  - runs `./build.ps1 restore`, `./build.ps1 build --skip restore`, then `./build.ps1 test-docker --skip build`
  - uploads TRX files from `release/TestResults/**/*.trx`
  - on failure, uploads Docker Compose logs as an artifact
- Make package publishing depend only on the existing Windows `build` leg unless the project policy should require Docker-test success before publishing. If Docker tests should gate publishing, split publish into its own job with `needs: [build, docker-tests]`.

### 5. Local Developer Workflow

- Document the local command in `CONTRIBUTING.md`, for example:
  - `./build.ps1 test` for unit tests only.
  - `./build.ps1 test-docker` for MySQL, PostgreSQL, and SQL Server integration tests.
- Note the prerequisites: Docker Desktop or Docker Engine with Compose V2, available ports `1433`, `3306`, and `5432`, and enough memory for SQL Server.
- Include cleanup guidance: `docker compose -f docker/docker-compose.yml down -v`.

### 6. Verification

- Run `./build.ps1 test` and confirm it still skips Docker-backed tests and passes without Docker running.
- Run `./build.ps1 test-docker` locally with Docker running and confirm all provider tests execute.
- Run the GitHub Actions workflow on a branch and confirm:
  - the existing OS matrix still runs unit tests and packaging,
  - the new Ubuntu Docker job starts all services,
  - all three provider test projects run,
  - test results and Docker logs are available as artifacts when appropriate.

## Open Decisions

- Whether Docker-backed tests must gate package publishing or can report as a separate required check.
- Whether to keep SQL Server coverage only on `net10.0` in Ubuntu CI, or add a separate Windows/manual path for `net481` coverage.
- Whether provider connection strings should remain hard-coded for tests or move to environment-variable overrides to avoid fixed host ports in local development.