# Contributing

## Testing

* Run `./build.ps1 test` to run the normal unit tests. This does not require Docker.
* Run `./build.ps1 test-docker` to run the MySQL, PostgreSQL, and SQL Server integration tests. This requires Docker Desktop or Docker Engine with Compose V2, available local ports `1433`, `3306`, and `5432`, and enough memory for SQL Server.
* The Docker-backed tests use the connection strings from `docker/docker-compose.yml` by default. Override them with `MUCHADO_MYSQL_TEST_CONNECTION_STRING`, `MUCHADO_NPGSQL_TEST_CONNECTION_STRING`, or `MUCHADO_SQLSERVER_TEST_CONNECTION_STRING` when using different local database endpoints.
* To clean up the database containers manually, run `docker compose -f docker/docker-compose.yml down -v`.

## Publishing

* To publish the library, update the `<VersionPrefix>` in [`Directory.Build.props`](Directory.Build.props), add a corresponding section to the top of [`ReleaseNotes.md`](ReleaseNotes.md), commit, and push.
