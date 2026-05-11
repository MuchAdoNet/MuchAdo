using FluentAssertions;
using Microsoft.Data.Sqlite;
using NUnit.Framework;
using static FluentAssertions.FluentActions;

namespace MuchAdo.Tests;

[TestFixture]
internal sealed class DbConnectorPoolTests
{
	[Test]
	public void NullSettings()
	{
		Invoking(() => new DbConnectorPool(null!)).Should().Throw<ArgumentNullException>();
	}

	[Test]
	public void NoCreateConnector()
	{
		Invoking(() => new DbConnectorPool(new DbConnectorPoolSettings())).Should().Throw<ArgumentException>();
	}

	[Test]
	public void Sync()
	{
		var createCount = 0;

		using var pool = new DbConnectorPool(new DbConnectorPoolSettings { CreateConnector = CreateConnector });

		using (var connector1 = pool.Get())
		using (var connector2 = pool.Get())
		{
			connector1.Command("select null;").QuerySingle<object>().Should().Be(null);
			connector2.Command("select null;").QuerySingle<object>().Should().Be(null);
		}
		using (var connector3 = pool.Get())
			connector3.Command("select null;").QuerySingle<object>().Should().Be(null);

		createCount.Should().Be(2);

		DbConnector CreateConnector()
		{
			createCount++;
			return new DbConnector(new SqliteConnection("Data Source=:memory:"));
		}
	}

	[Test]
	public async Task Async()
	{
		var createCount = 0;

		await using var pool = new DbConnectorPool(new DbConnectorPoolSettings { CreateConnector = CreateConnector });

		await using (var connector1 = pool.Get())
		await using (var connector2 = pool.Get())
		{
			(await connector1.Command("select null;").QuerySingleAsync<object>()).Should().Be(null);
			(await connector2.Command("select null;").QuerySingleAsync<object>()).Should().Be(null);
		}
		await using (var connector3 = pool.Get())
			(await connector3.Command("select null;").QuerySingleAsync<object>()).Should().Be(null);

		createCount.Should().Be(2);

		DbConnector CreateConnector()
		{
			createCount++;
			return new DbConnector(new SqliteConnection("Data Source=:memory:"));
		}
	}

	[Test]
	public void DoubleDisposeSync()
	{
		using var pool = new DbConnectorPool(new DbConnectorPoolSettings { CreateConnector = CreateConnector });

		var connector = pool.Get();
		connector.Command("select 1;").QuerySingle<int>().Should().Be(1);
		connector.Dispose();
		connector.Dispose();

		using var connector2 = pool.Get();
		connector2.Should().BeSameAs(connector);
		connector2.Command("select 1;").QuerySingle<int>().Should().Be(1);

		static DbConnector CreateConnector() => new(new SqliteConnection("Data Source=:memory:"));
	}

	[Test]
	public async Task DoubleDisposeAsync()
	{
		await using var pool = new DbConnectorPool(new DbConnectorPoolSettings { CreateConnector = CreateConnector });

		var connector = pool.Get();
		(await connector.Command("select 1;").QuerySingleAsync<int>()).Should().Be(1);
		await connector.DisposeAsync();
		await connector.DisposeAsync();

		await using var connector2 = pool.Get();
		connector2.Should().BeSameAs(connector);
		(await connector2.Command("select 1;").QuerySingleAsync<int>()).Should().Be(1);

		static DbConnector CreateConnector() => new(new SqliteConnection("Data Source=:memory:"));
	}

	[Test]
	public void AttachTransactionNoDisposeFlagDoesNotPersistAcrossPoolReuse()
	{
		using var pool = new DbConnectorPool(new DbConnectorPoolSettings { CreateConnector = CreateConnector });

		var connector = pool.Get();
		connector.Command("create table Items (Id int);").Execute();

		var transaction = ((SqliteConnection) connector.GetOpenConnection()).BeginTransaction();
		connector.AttachTransaction(transaction, noDispose: true);
		connector.Dispose();
		transaction.Dispose();

		using var connector2 = pool.Get();
		connector2.Should().BeSameAs(connector);

		connector2.BeginTransaction();
		connector2.Command("insert into Items (Id) values (1);").Execute();

		var transaction2 = connector2.Transaction;
		connector2.Dispose();

		using var connector3 = pool.Get();
		connector3.Should().BeSameAs(connector2);
		transaction2!.Connection.Should().BeNull("transaction should be disposed");

		static DbConnector CreateConnector() => new(new SqliteConnection("Data Source=:memory:"));
	}

	[Test]
	public async Task AttachTransactionNoDisposeFlagDoesNotPersistAcrossPoolReuseAsync()
	{
		await using var pool = new DbConnectorPool(new DbConnectorPoolSettings { CreateConnector = CreateConnector });

		var connector = pool.Get();
		(await connector.Command("create table Items (Id int);").ExecuteAsync()).Should().Be(0);

		var transaction = ((SqliteConnection) await connector.GetOpenConnectionAsync()).BeginTransaction();
		connector.AttachTransaction(transaction, noDispose: true);
		await connector.DisposeAsync();
		transaction.Dispose();

		await using var connector2 = pool.Get();
		connector2.Should().BeSameAs(connector);

		await connector2.BeginTransactionAsync();
		(await connector2.Command("insert into Items (Id) values (1);").ExecuteAsync()).Should().Be(1);

		var transaction2 = connector2.Transaction;
		await connector2.DisposeAsync();

		await using var connector3 = pool.Get();
		connector3.Should().BeSameAs(connector2);
		transaction2!.Connection.Should().BeNull("transaction should be disposed");

		static DbConnector CreateConnector() => new(new SqliteConnection("Data Source=:memory:"));
	}
}
