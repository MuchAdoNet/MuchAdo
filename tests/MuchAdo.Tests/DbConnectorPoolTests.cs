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
	public void NoCreate()
	{
		Invoking(() => new DbConnectorPool(new DbConnectorPoolSettings())).Should().Throw<ArgumentException>();
	}

	[Test]
	public void Sync()
	{
		var createCount = 0;

		using var pool = new DbConnectorPool(new DbConnectorPoolSettings { CreateConnector = CreateConnection });

		using (var connector1 = pool.Get())
		using (var connector2 = pool.Get())
		{
			connector1.Command("select null;").QuerySingle<object>().Should().Be(null);
			connector2.Command("select null;").QuerySingle<object>().Should().Be(null);
		}
		using (var connector3 = pool.Get())
			connector3.Command("select null;").QuerySingle<object>().Should().Be(null);

		createCount.Should().Be(2);

		DbConnector CreateConnection()
		{
			createCount++;
			return new DbConnector(new SqliteConnection("Data Source=:memory:"));
		}
	}

	[Test]
	public async Task Async()
	{
		var createCount = 0;

		await using var pool = new DbConnectorPool(new DbConnectorPoolSettings { CreateConnector = CreateConnection });

		await using (var connector1 = pool.Get())
		await using (var connector2 = pool.Get())
		{
			(await connector1.Command("select null;").QuerySingleAsync<object>()).Should().Be(null);
			(await connector2.Command("select null;").QuerySingleAsync<object>()).Should().Be(null);
		}
		await using (var connector3 = pool.Get())
			(await connector3.Command("select null;").QuerySingleAsync<object>()).Should().Be(null);

		createCount.Should().Be(2);

		DbConnector CreateConnection()
		{
			createCount++;
			return new DbConnector(new SqliteConnection("Data Source=:memory:"));
		}
	}
}
