using System.Data;
using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using NUnit.Framework;
using static FluentAssertions.FluentActions;

namespace MuchAdo.Tests;

[TestFixture]
[SuppressMessage("ReSharper", "InterpolatedStringExpressionIsNotIFormattable", Justification = "Custom formatting.")]
internal sealed class DbConnectorTests
{
	[Test]
	public void ArgumentNullException()
	{
		Invoking(() => new DbConnector(null!)).Should().Throw<ArgumentNullException>();

		using var connection = new SqliteConnection("Data Source=:memory:");
		Invoking(() => new DbConnector(connection, null!)).Should().Throw<ArgumentNullException>();
	}

	[Test]
	public void OpenConnection()
	{
		using var connector = new DbConnector(new SqliteConnection("Data Source=:memory:"));
		connector.Connection.State.Should().Be(ConnectionState.Closed);
		using (connector.OpenConnection())
		{
			connector.Connection.State.Should().Be(ConnectionState.Open);
			connector.Command("create table Items1 (ItemId integer primary key, Name text not null);").Execute().Should().Be(0);
			connector.Connection.State.Should().Be(ConnectionState.Open);
		}
		connector.Connection.State.Should().Be(ConnectionState.Closed);
	}

	[Test]
	public async Task OpenConnectionAsync()
	{
		await using var connector = new DbConnector(new SqliteConnection("Data Source=:memory:"));
		connector.Connection.State.Should().Be(ConnectionState.Closed);
		await using (await connector.OpenConnectionAsync())
		{
			connector.Connection.State.Should().Be(ConnectionState.Open);
			(await connector.Command("create table Items1 (ItemId integer primary key, Name text not null);").ExecuteAsync()).Should().Be(0);
			connector.Connection.State.Should().Be(ConnectionState.Open);
		}
		connector.Connection.State.Should().Be(ConnectionState.Closed);
	}

	[Test]
	public void GetOpenConnection()
	{
		using var connector = new DbConnector(new SqliteConnection("Data Source=:memory:"));
		connector.Connection.State.Should().Be(ConnectionState.Closed);
		var connection = connector.GetOpenConnection();
		connection.State.Should().Be(ConnectionState.Open);
		connection.Should().BeSameAs(connector.Connection);
		connector.CloseConnection();
		connection.State.Should().Be(ConnectionState.Closed);
		connector.CloseConnection();
	}

	[Test]
	public async Task GetOpenConnectionAsync()
	{
		await using var connector = new DbConnector(new SqliteConnection("Data Source=:memory:"));
		connector.Connection.State.Should().Be(ConnectionState.Closed);
		var connection = await connector.GetOpenConnectionAsync();
		connection.State.Should().Be(ConnectionState.Open);
		connection.Should().BeSameAs(connector.Connection);
		await connector.CloseConnectionAsync();
		connection.State.Should().Be(ConnectionState.Closed);
		await connector.CloseConnectionAsync();
	}

	[Test]
	public void AttachDisposable()
	{
		var ints = new List<int>();
		var disposable1 = new DisposableAction(() => ints.Add(1));
		var disposable2 = new AsyncDisposableAction(async () => ints.Add(2));

		var connector = new DbConnector(new SqliteConnection("Data Source=:memory:"));
		connector.AttachDisposable(disposable1);
		connector.AttachDisposable(disposable2);

		connector.Dispose();
		ints.Should().Equal(2, 1);

		connector.Dispose();
		ints.Should().Equal(2, 1);
	}

	[Test]
	public async Task AttachDisposableAsync()
	{
		var ints = new List<int>();
		var disposable1 = new DisposableAction(() => ints.Add(1));
		var disposable2 = new AsyncDisposableAction(async () => ints.Add(2));

		var connector = new DbConnector(new SqliteConnection("Data Source=:memory:"));
		connector.AttachDisposable(disposable1);
		connector.AttachDisposable(disposable2);

		await connector.DisposeAsync();
		ints.Should().Equal(2, 1);

		await connector.DisposeAsync();
		ints.Should().Equal(2, 1);
	}

	[Test]
	public void CommandTests()
	{
		using var connector = CreateConnector();
		connector.Command("create table Items (ItemId integer primary key, Name text not null);").Execute().Should().Be(0);
		connector.Command("insert into Items (Name) values ('item1'); insert into Items (Name) values ('item2');").Execute().Should().Be(2);
		connector.Command("select Name from Items order by ItemId;").Query<string>().Should().Equal("item1", "item2");
		connector.Command("select Name from Items order by ItemId;").Query(ToUpper).Should().Equal("ITEM1", "ITEM2");
		connector.Command("select Name from Items order by ItemId;").Enumerate<string>().Should().Equal("item1", "item2");
		connector.Command("select Name from Items order by ItemId;").Enumerate(ToUpper).Should().Equal("ITEM1", "ITEM2");
		connector.Command("select Name from Items order by ItemId;").QueryFirst<string>().Should().Be("item1");
		connector.Command("select Name from Items order by ItemId;").QueryFirst(ToUpper).Should().Be("ITEM1");
		connector.Command("select Name from Items order by ItemId;").QueryFirstOrDefault<string>().Should().Be("item1");
		connector.Command("select Name from Items order by ItemId;").QueryFirstOrDefault(ToUpper).Should().Be("ITEM1");
		connector.Command("select Name from Items order by ItemId limit 1;").QuerySingle<string>().Should().Be("item1");
		connector.Command("select Name from Items order by ItemId limit 1;").QuerySingle(ToUpper).Should().Be("ITEM1");
		connector.Command("select Name from Items order by ItemId limit 1;").QuerySingleOrDefault<string>().Should().Be("item1");
		connector.Command("select Name from Items order by ItemId limit 1;").QuerySingleOrDefault(ToUpper).Should().Be("ITEM1");
		connector.CreateCommandBatch().Command("select Name from Items order by ItemId limit 1;").QuerySingleOrDefault(ToUpper).Should().Be("ITEM1");
		Invoking(() => connector.CreateCommandBatch().QueryFirst<string>()).Should().Throw<InvalidOperationException>();
		Invoking(() => connector.Command("select Name from Items where Name = 'nope';").QueryFirst<string>()).Should().Throw<InvalidOperationException>();
		Invoking(() => connector.Command("select Name from Items;").QuerySingle<string>()).Should().Throw<InvalidOperationException>();
		connector.Command("select Name from Items where Name = 'nope';").QueryFirstOrDefault<string>().Should().BeNull();
		connector.Command("insert into Items (Name) values ('item1'); select last_insert_rowid();").QueryFirstOrDefault<long>().Should().NotBe(0);
		connector.Command("insert into Items (Name) values ('item1'); select last_insert_rowid();").QueryFirst<long>().Should().NotBe(0);
		connector.Command("insert into Items (Name) values ('item1'); select last_insert_rowid();").QuerySingleOrDefault<long>().Should().NotBe(0);
		connector.Command("insert into Items (Name) values ('item1'); select last_insert_rowid();").QuerySingle<long>().Should().NotBe(0);
	}

	[Test]
	public async Task CommandAsyncTests()
	{
		await using var connector = CreateConnector();
		(await connector.Command("create table Items (ItemId bigint primary key, Name text not null);").ExecuteAsync()).Should().Be(0);
		(await connector.Command("insert into Items (Name) values ('item1'); insert into Items (Name) values ('item2');").ExecuteAsync()).Should().Be(2);
		(await connector.Command("select Name from Items order by ItemId;").QueryAsync<string>()).Should().Equal("item1", "item2");
		(await connector.Command("select Name from Items order by ItemId;").QueryAsync(ToUpper)).Should().Equal("ITEM1", "ITEM2");
		(await ToListAsync(connector.Command("select Name from Items order by ItemId;").EnumerateAsync<string>())).Should().Equal("item1", "item2");
		(await ToListAsync(connector.Command("select Name from Items order by ItemId;").EnumerateAsync(ToUpper))).Should().Equal("ITEM1", "ITEM2");
		(await connector.Command("select Name from Items order by ItemId;").QueryFirstAsync<string>()).Should().Be("item1");
		(await connector.Command("select Name from Items order by ItemId;").QueryFirstAsync(ToUpper)).Should().Be("ITEM1");
		(await connector.Command("select Name from Items order by ItemId;").QueryFirstOrDefaultAsync<string>()).Should().Be("item1");
		(await connector.Command("select Name from Items order by ItemId;").QueryFirstOrDefaultAsync(ToUpper)).Should().Be("ITEM1");
		(await connector.Command("select Name from Items order by ItemId limit 1;").QuerySingleAsync<string>()).Should().Be("item1");
		(await connector.Command("select Name from Items order by ItemId limit 1;").QuerySingleAsync(ToUpper)).Should().Be("ITEM1");
		(await connector.Command("select Name from Items order by ItemId limit 1;").QuerySingleOrDefaultAsync<string>()).Should().Be("item1");
		(await connector.Command("select Name from Items order by ItemId limit 1;").QuerySingleOrDefaultAsync(ToUpper)).Should().Be("ITEM1");
		(await connector.CreateCommandBatch().Command("select Name from Items order by ItemId limit 1;").QuerySingleOrDefaultAsync(ToUpper)).Should().Be("ITEM1");
		await Invoking(async () => await connector.CreateCommandBatch().QueryFirstAsync<string>()).Should().ThrowAsync<InvalidOperationException>();
		await Invoking(async () => await connector.Command("select Name from Items where Name = 'nope';").QueryFirstAsync<string>()).Should().ThrowAsync<InvalidOperationException>();
		await Invoking(async () => await connector.Command("select Name from Items;").QuerySingleAsync<string>()).Should().ThrowAsync<InvalidOperationException>();
		(await connector.Command("select Name from Items where Name = 'nope';").QueryFirstOrDefaultAsync<string>()).Should().BeNull();
		(await connector.Command("insert into Items (Name) values ('item1'); select last_insert_rowid();").QueryFirstOrDefaultAsync<long>()).Should().NotBe(0);
		(await connector.Command("insert into Items (Name) values ('item1'); select last_insert_rowid();").QueryFirstAsync<long>()).Should().NotBe(0);
		(await connector.Command("insert into Items (Name) values ('item1'); select last_insert_rowid();").QuerySingleOrDefaultAsync<long>()).Should().NotBe(0);
		(await connector.Command("insert into Items (Name) values ('item1'); select last_insert_rowid();").QuerySingleAsync<long>()).Should().NotBe(0);
	}

	[Test]
	public void CommandInTransactionTests()
	{
		using var connector = CreateConnector();
		connector.Command("create table Items (ItemId integer primary key, Name text not null);").Execute().Should().Be(0);
		connector.Command("insert into Items (Name) values ('item1');").InTransaction().Execute().Should().Be(1);
		connector.Command("insert into Items (Name) values ('item2'); select last_insert_rowid();").InTransaction().QuerySingle<int>().Should().BeGreaterThan(0);
		connector.Command("insert into Items (Name) values ('item3'); select last_insert_rowid(); insert into Items (Name) values ('item4'); select last_insert_rowid();").InTransaction().Query<int>().Should().HaveCount(2);
		connector.Command("insert into Items (Name) values ('item5'); select last_insert_rowid(); insert into Items (Name) values ('item6'); select last_insert_rowid();").InTransaction().QueryMultiple(x => x.ReadSingle<int>() + x.ReadSingle<int>()).Should().BeGreaterThan(0);
		Invoking(() => connector.Command("insert into Items (Name) values ('item3'); select last_insert_rowid();").InTransaction().Enumerate<int>().Single()).Should().Throw<InvalidOperationException>();
		connector.Command("select Name from Items order by ItemId;").Query<string>().Should().HaveCount(6);
	}

	[Test]
	public async Task CommandInTransactionAsyncTests()
	{
		await using var connector = CreateConnector();
		(await connector.Command("create table Items (ItemId integer primary key, Name text not null);").ExecuteAsync()).Should().Be(0);
		(await connector.Command("insert into Items (Name) values ('item1');").InTransaction().ExecuteAsync()).Should().Be(1);
		(await connector.Command("insert into Items (Name) values ('item2'); select last_insert_rowid();").InTransaction().QuerySingleAsync<int>()).Should().BeGreaterThan(0);
		(await connector.Command("insert into Items (Name) values ('item3'); select last_insert_rowid(); insert into Items (Name) values ('item4'); select last_insert_rowid();").InTransaction().QueryAsync<int>()).Should().HaveCount(2);
		(await connector.Command("insert into Items (Name) values ('item5'); select last_insert_rowid(); insert into Items (Name) values ('item6'); select last_insert_rowid();").InTransaction().QueryMultipleAsync(async x => await x.ReadSingleAsync<int>() + await x.ReadSingleAsync<int>())).Should().BeGreaterThan(0);
		await Awaiting(async () => await ToListAsync(connector.Command("insert into Items (Name) values ('item3'); select last_insert_rowid();").InTransaction().EnumerateAsync<int>())).Should().ThrowAsync<InvalidOperationException>();
		(await connector.Command("select Name from Items order by ItemId;").QueryAsync<string>()).Should().HaveCount(6);
	}

	[Test]
	public void ParametersTests()
	{
		using var connector = CreateConnector();
		connector.Command("create table Items (ItemId integer primary key, Name text not null);").Execute().Should().Be(0);
		connector.Command("insert into Items (Name) values (@item1); insert into Items (Name) values (@item2);", Sql.NamedParam("item1", "one"), Sql.NamedParam("item2", "two")).Execute().Should().Be(2);
		connector.Command("select Name from Items where Name like @like;", Sql.NamedParam("like", "t%")).QueryFirst<string>().Should().Be("two");
	}

	[Test]
	public async Task ParametersAsyncTests()
	{
		await using var connector = CreateConnector();
		(await connector.Command("create table Items (ItemId integer primary key, Name text not null);").ExecuteAsync()).Should().Be(0);
		(await connector.Command("insert into Items (Name) values (@item1); insert into Items (Name) values (@item2);", Sql.NamedParam("item1", "one"), Sql.NamedParam("item2", "two")).ExecuteAsync()).Should().Be(2);
		(await connector.Command("select Name from Items where Name like @like;", Sql.NamedParam("like", "t%")).QueryFirstAsync<string>()).Should().Be("two");
	}

	[Test]
	public void ParametersFromDtoTests()
	{
		using var connector = CreateConnector();
		const string item1 = "one";
		const string item2 = "two";
		connector.Command("create table Items (ItemId integer primary key, Name text not null);").Execute().Should().Be(0);
		connector.Command("insert into Items (Name) values (@item1); insert into Items (Name) values (@item2);", Sql.DtoNamedParams(new { item1, item2 })).Execute().Should().Be(2);
		connector.Command("select Name from Items order by ItemId;").Query<string>().Should().Equal(item1, item2);
	}

	[Test]
	public void ParametersFromSqlTests()
	{
		using var connector = CreateConnector();
		connector.Command("create table Items (ItemId integer primary key, Name text not null);").Execute().Should().Be(0);
		var item1 = "two";
		var item2 = "t_o";
		connector.CommandFormat(
			$"insert into Items (Name) values ({item1}); insert into Items (Name) values ({item2});").Execute().Should().Be(2);
		connector.CommandFormat(
			$@"select Name from Items where Name like {Sql.LikeParamStartsWith("t_")} escape '\';").QuerySingle<string>().Should().Be("t_o");
	}

	[Test]
	public void PrepareTests()
	{
		using var connector = CreateConnector();
		var createCmd = connector.Command("create table Items (ItemId integer primary key, Name text not null);");
		createCmd.IsPrepared.Should().Be(null);
		createCmd.Execute().Should().Be(0);

		string insertStmt = "insert into Items (Name) values (@item);";
		var preparedCmd = connector.Command(insertStmt, Sql.NamedParam("item", "one")).Prepare();
		preparedCmd.IsPrepared.Should().Be(true);
		preparedCmd.Execute().Should().Be(1);

		connector.Command(insertStmt, Sql.NamedParam("item", "two")).Execute().Should().Be(1);
		connector.Command("select Name from Items order by ItemId;").Query<string>().Should().Equal("one", "two");
	}

	[Test]
	public void PrepareCacheTests()
	{
		using var connector = CreateConnector();
		connector.Command("create table Items (ItemId integer primary key, Name text not null);").Execute();

		var insertStmt = "insert into Items (Name) values (@item);";
		connector.Command(insertStmt, Sql.NamedParam("item", "one")).Prepare().Cache().Execute().Should().Be(1);
		connector.Command(insertStmt, Sql.NamedParam("item", "two")).Prepare().Cache().Execute().Should().Be(1);

		connector.Command("select Name from Items order by ItemId;").Query<string>().Should().Equal("one", "two");
	}

	[Test]
	public async Task PrepareCacheTestsAsync()
	{
		await using var connector = CreateConnector();
		await connector.Command("create table Items (ItemId integer primary key, Name text not null);").ExecuteAsync();

		var insertStmt = "insert into Items (Name) values (@item);";
		(await connector.Command(insertStmt, Sql.NamedParam("item", "one")).Prepare().Cache().ExecuteAsync()).Should().Be(1);
		(await connector.Command(insertStmt, Sql.NamedParam("item", "two")).Prepare().Cache().ExecuteAsync()).Should().Be(1);

		(await connector.Command("select Name from Items order by ItemId;").QueryAsync<string>()).Should().Equal("one", "two");
	}

	[Test]
	public void TransactionTests([Values] bool? commit)
	{
		using var connector = CreateConnector();
		connector.Command("create table Items (ItemId integer primary key, Name text not null);").Execute();

		using (connector.BeginTransaction())
		{
			connector.Command("insert into Items (Name) values ('item1');").Execute();
			if (commit == true)
				connector.CommitTransaction();
			else if (commit == false)
				connector.RollbackTransaction();
		}

		connector.Command("select count(*) from Items;").QueryFirst<long>().Should().Be(commit == true ? 1 : 0);
	}

	[Test]
	public async Task TransactionAsyncTests([Values] bool? commit)
	{
		await using var connector = CreateConnector();
		await connector.Command("create table Items (ItemId integer primary key, Name text not null);").ExecuteAsync();

		await using (await connector.BeginTransactionAsync())
		{
			await connector.Command("insert into Items (Name) values ('item1');").ExecuteAsync();
			if (commit == true)
				await connector.CommitTransactionAsync();
			else if (commit == false)
				await connector.RollbackTransactionAsync();
		}

		(await connector.Command("select count(*) from Items;").QueryFirstAsync<long>()).Should().Be(commit == true ? 1 : 0);
	}

	[Test]
	public void IsolationLevelTests()
	{
		using var connector = CreateConnector();
		connector.Command("create table Items (ItemId integer primary key, Name text not null);").Execute();

		using (connector.BeginTransaction(IsolationLevel.ReadCommitted))
		{
			connector.Command("insert into Items (Name) values ('item1');").Execute();
			connector.CommitTransaction();
		}

		connector.Command("select count(*) from Items;").QueryFirst<long>().Should().Be(1);
	}

	[Test]
	public async Task IsolationLevelAsyncTests()
	{
		await using var connector = CreateConnector();
		await connector.Command("create table Items (ItemId integer primary key, Name text not null);").ExecuteAsync();

		await using (await connector.BeginTransactionAsync(IsolationLevel.ReadCommitted))
		{
			await connector.Command("insert into Items (Name) values ('item2');").ExecuteAsync();
			await connector.CommitTransactionAsync();
		}

		connector.Command("select count(*) from Items;").QueryFirst<long>().Should().Be(1);
	}

	[Test]
	public void ExecuteInTransactionTests([Values] bool? commit)
	{
		using var connector = CreateConnector();
		connector.Command("create table Items (ItemId integer primary key, Name text not null);").Execute();

		connector.ExecuteInTransaction(() =>
		{
			connector.Command("insert into Items (Name) values ('item1');").Execute();
			if (commit == true)
				connector.CommitTransaction();
			else if (commit == false)
				connector.RollbackTransaction();
		});

		connector.ExecuteInTransaction(IsolationLevel.RepeatableRead, () =>
		{
			connector.Command("insert into Items (Name) values ('item1');").Execute();
			if (commit == true)
				connector.CommitTransaction();
			else if (commit == false)
				connector.RollbackTransaction();
		});

		connector.ExecuteInTransaction(() => connector.Command("select count(*) from Items;").QueryFirst<long>())
			.Should().Be(commit != false ? 2 : 0);

		connector.ExecuteInTransaction(IsolationLevel.RepeatableRead, () => connector.Command("select count(*) from Items;").QueryFirst<long>())
			.Should().Be(commit != false ? 2 : 0);
	}

	[Test]
	public async Task ExecuteInTransactionAsyncTests([Values] bool? commit)
	{
		await using var connector = CreateConnector();
		await connector.Command("create table Items (ItemId integer primary key, Name text not null);").ExecuteAsync();

		await connector.ExecuteInTransactionAsync(async () =>
		{
			await connector.Command("insert into Items (Name) values ('item1');").ExecuteAsync();
			if (commit == true)
				await connector.CommitTransactionAsync();
			else if (commit == false)
				await connector.RollbackTransactionAsync();
		});

		await connector.ExecuteInTransactionAsync(IsolationLevel.RepeatableRead, async () =>
		{
			await connector.Command("insert into Items (Name) values ('item1');").ExecuteAsync();
			if (commit == true)
				await connector.CommitTransactionAsync();
			else if (commit == false)
				await connector.RollbackTransactionAsync();
		});

		await connector.ExecuteInTransactionAsync(async () =>
		{
			(await connector.Command("select count(*) from Items;").QueryFirstAsync<long>()).Should().Be(commit != false ? 2 : 0);
		});

		var count = await connector.ExecuteInTransactionAsync(
			async () => (await connector.Command("select count(*) from Items;").QueryFirstAsync<long>()));
		count.Should().Be(commit != false ? 2 : 0);

		count = await connector.ExecuteInTransactionAsync(IsolationLevel.RepeatableRead,
			async () => (await connector.Command("select count(*) from Items;").QueryFirstAsync<long>()));
		count.Should().Be(commit != false ? 2 : 0);
	}

	[Test]
	public void CachedWithTransaction()
	{
		using var connector = CreateConnector();
		connector.Command("create table Items (ItemId integer primary key, Name text not null);").Execute();

		// make sure correct transaction is used in cached command
		foreach (var item in new[] { "one", "two" })
		{
			using (connector.BeginTransaction())
			{
				connector.Command("insert into Items (Name) values (@item);", Sql.NamedParam("item", item)).Prepare().Cache().Execute().Should().Be(1);
				connector.CommitTransaction();
			}
		}

		connector.Command("insert into Items (Name) values (@item);", Sql.NamedParam("item", "three")).Prepare().Cache().Execute().Should().Be(1);

		connector.Command("select Name from Items order by ItemId;").Query<string>().Should().Equal("one", "two", "three");
	}

	[Test]
	public void DeferredTransaction()
	{
		var connectionString = new SqliteConnectionStringBuilder { DataSource = nameof(DeferredTransaction), Mode = SqliteOpenMode.Memory, Cache = SqliteCacheMode.Shared }.ConnectionString;
		using var connector1 = new DbConnector(new SqliteConnection(connectionString));
		using var connector2 = new DbConnector(new SqliteConnection(connectionString));
		((SqliteConnection) connector1.Connection).DefaultTimeout = 5;
		((SqliteConnection) connector2.Connection).DefaultTimeout = 5;
		connector1.Command("create table Items (ItemId integer primary key, Name text not null);").Execute();
		connector1.Command("insert into Items (Name) values ('xyzzy');").Execute();
		using var transaction1 = connector1.AttachTransaction(((SqliteConnection) connector1.GetOpenConnection()).BeginTransaction(deferred: true));
		using var transaction2 = connector2.AttachTransaction(((SqliteConnection) connector2.GetOpenConnection()).BeginTransaction(deferred: true));
		connector1.Command("select count(*) from Items;").QuerySingle<long>().Should().Be(1);
		connector2.Command("select count(*) from Items;").QuerySingle<long>().Should().Be(1);
		connector1.CommitTransaction();
	}

	[Test]
	public void QueryMultipleTests()
	{
		using var connector = CreateConnector();
		connector.Command("create table Items (ItemId integer primary key, Name text not null);").Execute();
		connector.Command("insert into Items (Name) values ('item1'), ('item2');").Execute();

		const string sql = """
			select ItemId from Items order by Name;
			select ItemId from Items where Name = 'item2';
			select ItemId from Items where Name = 'item1';
			""";

		using (var reader = connector.Command(sql).QueryMultiple())
		{
			var id1 = reader.Read<long>().First();
			var id2 = reader.Read(x => x.Get<long>()).Single();
			var id3 = reader.Read(x => x.Get<long>()).Single();
			id1.Should().BeLessThan(id2);
			id1.Should().Be(id3);
			Invoking(() => reader.Read(x => 0)).Should().Throw<InvalidOperationException>();
		}

		using (var reader = connector.Command(sql).QueryMultiple())
		{
			var id1 = reader.Enumerate<long>().First();
			var id2 = reader.Enumerate(x => x.Get<long>()).Single();
			var id3 = reader.Enumerate(x => x.Get<long>()).Single();
			id1.Should().BeLessThan(id2);
			id1.Should().Be(id3);
			Invoking(() => reader.Enumerate(x => 0).Count()).Should().Throw<InvalidOperationException>();
		}

		using (var reader = connector.Command(sql).QueryMultiple())
		{
			var id1 = reader.ReadFirst<long>();
			var id2 = reader.ReadSingle(x => x.Get<long>());
			var id3 = reader.ReadSingle(x => x.Get<long>());
			id1.Should().BeLessThan(id2);
			id1.Should().Be(id3);
			Invoking(() => reader.ReadFirst(x => 0)).Should().Throw<InvalidOperationException>();
		}

		var tuple = connector.Command(sql).QueryMultiple(reader =>
		{
			var id1 = reader.ReadFirst<long>();
			var id2 = reader.ReadSingle(x => x.Get<long>());
			var id3 = reader.ReadSingle(x => x.Get<long>());
			Invoking(() => reader.ReadFirst(x => 0)).Should().Throw<InvalidOperationException>();
			return (id1, id2, id3);
		});
		tuple.id1.Should().BeLessThan(tuple.id2);
		tuple.id1.Should().Be(tuple.id3);
	}

	[Test]
	public async Task QueryMultipleAsyncTests()
	{
		await using var connector = CreateConnector();
		await connector.Command("create table Items (ItemId integer primary key, Name text not null);").ExecuteAsync();
		await connector.Command("insert into Items (Name) values ('item1'), ('item2');").ExecuteAsync();

		const string sql = """
			select ItemId from Items order by Name;
			select ItemId from Items where Name = 'item2';
			select ItemId from Items where Name = 'item1';
			""";

		await using (var reader = await connector.Command(sql).QueryMultipleAsync())
		{
			var id1 = (await reader.ReadAsync<long>()).First();
			var id2 = (await reader.ReadAsync(x => x.Get<long>())).Single();
			var id3 = (await reader.ReadAsync(x => x.Get<long>())).Single();
			id1.Should().BeLessThan(id2);
			id1.Should().Be(id3);
			await Awaiting(async () => await reader.ReadAsync(x => 0)).Should().ThrowAsync<InvalidOperationException>();
		}

		await using (var reader = await connector.Command(sql).QueryMultipleAsync())
		{
			var id1 = await FirstAsync(reader.EnumerateAsync<long>());
			var id2 = await FirstAsync(reader.EnumerateAsync(x => x.Get<long>()));
			var id3 = await FirstAsync(reader.EnumerateAsync(x => x.Get<long>()));
			id1.Should().BeLessThan(id2);
			id1.Should().Be(id3);
			await Awaiting(async () => await ToListAsync(reader.EnumerateAsync(x => 0))).Should().ThrowAsync<InvalidOperationException>();
		}

		await using (var reader = await connector.Command(sql).QueryMultipleAsync())
		{
			var id1 = await reader.ReadFirstAsync<long>();
			var id2 = await reader.ReadSingleAsync(x => x.Get<long>());
			var id3 = await reader.ReadSingleAsync(x => x.Get<long>());
			id1.Should().BeLessThan(id2);
			id1.Should().Be(id3);
			await Awaiting(async () => await reader.ReadFirstAsync(x => 0)).Should().ThrowAsync<InvalidOperationException>();
		}

		var tuple = await connector.Command(sql).QueryMultipleAsync(async reader =>
		{
			var id1 = await reader.ReadFirstAsync<long>();
			var id2 = await reader.ReadSingleAsync(x => x.Get<long>());
			var id3 = await reader.ReadSingleAsync(x => x.Get<long>());
			await Awaiting(async () => await reader.ReadFirstAsync(x => 0)).Should().ThrowAsync<InvalidOperationException>();
			return (id1, id2, id3);
		});
		tuple.id1.Should().BeLessThan(tuple.id2);
		tuple.id1.Should().Be(tuple.id3);
	}

	[Test]
	public void CacheTests()
	{
		using var connector = CreateConnector();
		connector.Command("create table Items (ItemId integer primary key, Name text not null);").Execute().Should().Be(0);
		foreach (var name in new[] { "one", "two", "three" })
			connector.Command("insert into Items (Name) values (@name);", Sql.NamedParam("name", name)).Cache().Execute().Should().Be(1);
		connector.Command("select Name from Items order by ItemId;").Query<string>().Should().Equal("one", "two", "three");
	}

	[Test]
	public void CachedParameterErrors()
	{
		using var connector = CreateConnector();
		connector.Command("create table Items (ItemId integer primary key, Name text not null);").Execute();
		var sql = "insert into Items (Name) values (@name);";
		connector.Command(sql, Sql.NamedParam("name", "one")).Cache().Execute().Should().Be(1);
		Invoking(() => connector.Command(sql, Sql.NamedParam("three", "four"), Sql.NamedParam("name", "two")).Cache().Execute()).Should().Throw<InvalidOperationException>();
		Invoking(() => connector.Command(sql, Sql.NamedParam("title", "three")).Cache().Execute()).Should().Throw<InvalidOperationException>();
		connector.Command("select Name from Items order by ItemId;").Query<string>().Should().Equal("one");
	}

	[Test]
	public async Task CacheAsyncTests()
	{
		await using var connector = CreateConnector();
		(await connector.Command("create table Items (ItemId integer primary key, Name text not null);").ExecuteAsync()).Should().Be(0);
		foreach (var name in new[] { "one", "two", "three" })
			(await connector.Command("insert into Items (Name) values (@name);", Sql.NamedParam("name", name)).Cache().ExecuteAsync()).Should().Be(1);
		(await connector.Command("select Name from Items order by ItemId;").QueryAsync<string>()).Should().Equal("one", "two", "three");
	}

	[Test]
	public void StoredProcedureUnitTests()
	{
		using var connector = CreateConnector();
		var createCommand = connector.Command("create table Items (ItemId integer primary key, Name text not null);");
		createCommand.GetCommand(0).Type.Should().Be(CommandType.Text);
		createCommand.Execute().Should().Be(0);
		connector.Command("insert into Items (Name) values (@item1);", Sql.NamedParam("item1", "one")).GetCommand(0).Type.Should().Be(CommandType.Text);

		var storedProcedureCommand = connector.StoredProcedure("values (1);");
		storedProcedureCommand.GetCommand(0).Type.Should().Be(CommandType.StoredProcedure);
		Invoking(storedProcedureCommand.Execute).Should().Throw<ArgumentException>("CommandType must be Text. (Parameter 'value')");
		connector.StoredProcedure("values (@two);", Sql.NamedParam("two", 2)).GetCommand(0).Type.Should().Be(CommandType.StoredProcedure);
	}

	[Test]
	public void TimeoutUnitTests()
	{
		var connector = CreateConnector();
		connector.Command("create table Items (ItemId integer primary key, Name text not null);").Execute();
		connector.Command("insert into Items (Name) values ('xyzzy'), ('abccb');").Execute();

		var command = connector.Command("select Name from Items;");

		command.Timeout.Should().Be(null);

		Invoking(() => command.WithTimeout(TimeSpan.FromSeconds(-10))).Should().Throw<ArgumentOutOfRangeException>();
		Invoking(() => command.WithTimeout(TimeSpan.FromSeconds(0))).Should().Throw<ArgumentOutOfRangeException>();

		var oneMinuteCommand = command.WithTimeout(TimeSpan.FromMinutes(1));
		oneMinuteCommand.Timeout.Should().Be(TimeSpan.FromMinutes(1));
		foreach (var name in oneMinuteCommand.Enumerate<string>())
			connector.ActiveCommand!.CommandTimeout.Should().Be(60);
		var halfSecondCommand = command.WithTimeout(TimeSpan.FromMilliseconds(500));
		halfSecondCommand.Timeout.Should().Be(TimeSpan.FromMilliseconds(500));
		foreach (var name in halfSecondCommand.Enumerate<string>())
			connector.ActiveCommand!.CommandTimeout.Should().Be(1);
		var noTimeoutCommand = command.WithTimeout(Timeout.InfiniteTimeSpan);
		noTimeoutCommand.Timeout.Should().Be(Timeout.InfiniteTimeSpan);
		foreach (var name in noTimeoutCommand.Enumerate<string>())
			connector.ActiveCommand!.CommandTimeout.Should().Be(0);
	}

	[Test]
	public void TimeoutTest()
	{
		var connectionString = new SqliteConnectionStringBuilder { DataSource = nameof(TimeoutTest), Mode = SqliteOpenMode.Memory, Cache = SqliteCacheMode.Shared }.ConnectionString;
		using var connector1 = new DbConnector(new SqliteConnection(connectionString));
		using var connector2 = new DbConnector(new SqliteConnection(connectionString));
		connector1.Command("create table Items (ItemId integer primary key, Name text not null);").Execute();
		connector2.Command("insert into Items (Name) values ('xyzzy');").Execute();
		using var transaction1 = connector1.BeginTransaction();
		Invoking(() => connector2.Command("insert into Items (Name) values ('querty');").WithTimeout(TimeSpan.FromSeconds(1)).Execute()).Should().Throw<SqliteException>();
	}

	[TestCase(true)]
	[TestCase(false)]
	public void EnumQueryTests(bool flexible)
	{
		using var connector = CreateConnector(DbDataMapper.Default.WithAllowStringToEnum(flexible));
		connector.Command("create table Items (ItemId integer primary key, Name text null, Number integer null);").Execute();
		connector.Command("insert into Items (Name, Number) values ('Ordinal', 4), ('ordinal', null), (null, 4), ('fail', null);").Execute();

		connector.Command("select Number from Items order by ItemId limit 1;")
			.QuerySingle<StringComparison>()
			.Should().Be(StringComparison.Ordinal);
		connector.Command("select Number from Items order by ItemId limit 1 offset 1;")
			.QuerySingle<StringComparison?>()
			.Should().Be(null);

		if (flexible)
		{
			connector.Command("select Name, Number from Items order by ItemId limit 1;")
				.QuerySingle<(StringComparison, StringComparison)>()
				.Should().Be((StringComparison.Ordinal, StringComparison.Ordinal));
			connector.Command("select Name, Number from Items order by ItemId limit 1 offset 1;")
				.QuerySingle<(StringComparison, StringComparison?)>()
				.Should().Be((StringComparison.Ordinal, null));
			connector.Command("select Name, Number from Items order by ItemId limit 1 offset 2;")
				.QuerySingle<(StringComparison?, StringComparison)>()
				.Should().Be((null, StringComparison.Ordinal));
			Invoking(() => connector.Command("select Name, Number from Items order by ItemId limit 1 offset 3;")
					.QuerySingle<(StringComparison?, StringComparison?)>())
				.Should().Throw<InvalidOperationException>();
		}
		else
		{
			// SQLite reads strings as zero
			connector.Command("select Name from Items order by ItemId limit 1;")
				.QuerySingle<StringComparison>()
				.Should().Be(default);
		}
	}

	[Test]
	public void ExplicitParameterTests()
	{
		using var connector = CreateConnector();
		connector.Command("create table Items (ItemId integer primary key, Name text null, Number integer null);").Execute();
		connector.Command("insert into Items (Name, Number) values (@Name, @Number);", Sql.NamedParam("Name", 'A'), Sql.NamedParam("Number", 'A')).Execute();
		connector.Command("select Name, Number from Items order by ItemId limit 1;").QuerySingle<(string, string)>().Should().Be(("A", "A"));
		connector.Command("insert into Items (Name, Number) values (@Name, @Number);",
			Sql.NamedParam("Name", new SqliteParameter { Value = 'A', SqliteType = SqliteType.Text }),
			Sql.NamedParam("Number", new SqliteParameter { Value = 'A', SqliteType = SqliteType.Integer })).Execute();
		connector.Command("select Name, Number from Items order by ItemId limit 1 offset 1;").QuerySingle<(string, long)>().Should().Be(("A", 65L));
	}

	[Test]
	public void ParameterSizeTests()
	{
		using var connector = CreateConnector();
		connector.Command("create table Items (ItemId integer primary key, Name text null);").Execute();
		connector.Command("insert into Items (Name) values (@Name);",
			Sql.NamedParam("Name", "1234567890", SqlParamType.Create(x => x.Size = 5))).Execute();
		connector.Command("select Name from Items order by ItemId limit 1;").QuerySingle<string>().Should().Be("12345");
	}

	[Test]
	public void ReleaseConnection()
	{
		// if connection was not closed and reopened, the temporary table would still exist and recreating it would fail
		using var connector = CreateConnector();
		connector.Command("create temporary table Items (ItemId integer primary key);").Execute().Should().Be(0);
		connector.CloseConnection();
		connector.Command("create temporary table Items (ItemId integer primary key);").Execute().Should().Be(0);
	}

	[Test]
	public async Task ReleaseConnectionAsync()
	{
		// if connection was not closed and reopened, the temporary table would still exist and recreating it would fail
		await using var connector = CreateConnector();
		(await connector.Command("create temporary table Items (ItemId integer primary key);").ExecuteAsync()).Should().Be(0);
		await connector.CloseConnectionAsync();
		(await connector.Command("create temporary table Items (ItemId integer primary key);").ExecuteAsync()).Should().Be(0);
	}

	private static async Task<IReadOnlyList<T>> ToListAsync<T>(IAsyncEnumerable<T> items)
	{
		var list = new List<T>();
		await foreach (var item in items)
			list.Add(item);
		return list;
	}

	private static async Task<T> FirstAsync<T>(IAsyncEnumerable<T> items)
	{
		await foreach (var item in items)
			return item;
		throw new InvalidOperationException();
	}

	private sealed class DisposableAction(Action action) : IDisposable
	{
		public void Dispose() => action();
	}

	private sealed class AsyncDisposableAction(Func<ValueTask> asyncAction) : IAsyncDisposable
	{
		public ValueTask DisposeAsync() => asyncAction();
	}

	private static DbConnector CreateConnector(DbDataMapper? dataMapper = null) =>
		new(new SqliteConnection("Data Source=:memory:"),
			new DbConnectorSettings
			{
				DataMapper = dataMapper ?? DbDataMapper.Default,
			});

	private static string ToUpper(DbConnectorRecord x) => x.Get<string>().ToUpperInvariant();
}
