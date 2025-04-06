#!/bin/bash
echo 'setup.sh: create mssql test database'
docker exec muchado-mssql-1 /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P P@ssw0rd -Q "create database test;"
echo 'setup.sh: create mysql test database'
docker exec muchado-mysql-1 mysql -uroot -ptest -e "create schema test collate utf8mb4_bin;"
echo 'setup.sh: create postgres test database'
docker exec -e PGPASSWORD=test muchado-postgres-1 psql -U root -c "CREATE DATABASE test;"
echo 'done'
