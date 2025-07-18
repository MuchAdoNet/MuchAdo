using FluentAssertions;
using Microsoft.Data.Sqlite;
using MuchAdo.Polly;
using NUnit.Framework;
using Polly;
using static FluentAssertions.FluentActions;

namespace MuchAdo.Tests;

[TestFixture]
internal sealed class DbRetryPolicyTests
{
	[Test]
	public void ConnectionRetryPolicy_NotImplemented()
	{
		var settings = new DbConnectorSettings { RetryPolicy = new FakeDbRetryPolicy() };
		using var connector = new DbConnector(new SqliteConnection("Data Source=:memory:"), settings);
		Invoking(() => connector.Command("select 1;").QuerySingle<int>()).Should().Throw<NotImplementedException>();
	}

	[Test]
	public void ConnectionRetryPolicy_EmptyResiliencePipeline()
	{
		var settings = new DbConnectorSettings { RetryPolicy = PollyDbRetryPolicy.Create(ResiliencePipeline.Empty) };
		using var connector = new DbConnector(new SqliteConnection("Data Source=:memory:"), settings);
		connector.Command("select 1;").QuerySingle<int>().Should().Be(1);
	}

	private sealed class FakeDbRetryPolicy : DbRetryPolicy
	{
		protected override void ExecuteCore(DbConnector connector, Action action) => throw new NotImplementedException();

		protected override async ValueTask ExecuteCoreAsync(DbConnector connector, Func<CancellationToken, ValueTask> action, CancellationToken cancellationToken = default) => throw new NotImplementedException();
	}
}
