#!/bin/bash
set -euo pipefail

password=${MSSQL_SA_PASSWORD:-P@ssw0rd}
sqlcmd=

for candidate in /opt/mssql-tools18/bin/sqlcmd /opt/mssql-tools/bin/sqlcmd; do
	if [[ -x "$candidate" ]]; then
		sqlcmd=$candidate
		break
	fi
done

if [[ -z "$sqlcmd" ]]; then
	echo 'setup.sh: sqlcmd not found' >&2
	exit 1
fi

run_sqlcmd() {
	"$sqlcmd" -S mssql -U sa -P "$password" -C "$@" ||
		"$sqlcmd" -S mssql -U sa -P "$password" "$@"
}

echo 'setup.sh: waiting for mssql'
for attempt in {1..60}; do
	if run_sqlcmd -Q 'select 1' >/dev/null 2>&1; then
		break
	fi

	if [[ $attempt -eq 60 ]]; then
		echo 'setup.sh: mssql did not become ready' >&2
		exit 1
	fi

	sleep 2
done

echo 'setup.sh: create mssql test database'
run_sqlcmd -Q "if db_id(N'test') is null create database [test];"
echo 'done'
