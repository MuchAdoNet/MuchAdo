using FluentAssertions;
using Microsoft.Data.Sqlite;
using NUnit.Framework;

namespace MuchAdo.Sqlite.Tests;

[TestFixture]
internal sealed class SqliteTests
{
	[Test]
	public void DeferredTransaction()
	{
		var connectionString = new SqliteConnectionStringBuilder { DataSource = nameof(DeferredTransaction), Mode = SqliteOpenMode.Memory, Cache = SqliteCacheMode.Shared }.ConnectionString;
		var connectorSettings = new SqliteDbConnectorSettings { DefaultTimeout = TimeSpan.FromSeconds(5) };
		using var connector1 = new SqliteDbConnector(new SqliteConnection(connectionString), connectorSettings);
		using var connector2 = new SqliteDbConnector(new SqliteConnection(connectionString), connectorSettings);
		connector1.Command("create table Items (ItemId integer primary key, Name text not null);").Execute();
		connector1.Command("insert into Items (Name) values ('xyzzy');").Execute();
		using var transaction1 = connector1.BeginTransaction(deferred: true);
		using var transaction2 = connector2.BeginTransaction(deferred: true);
		connector1.Command("select count(*) from Items;").QuerySingle<long>().Should().Be(1);
		connector2.Command("select count(*) from Items;").QuerySingle<long>().Should().Be(1);
		connector1.CommitTransaction();
	}
}
