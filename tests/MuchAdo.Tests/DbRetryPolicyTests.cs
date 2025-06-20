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
	public void RetryOpenPolicy_NotImplemented()
	{
		var settings = new DbConnectorSettings { RetryOpenPolicy = new FakeDbRetryPolicy() };
		using var connector = new DbConnector(new SqliteConnection("Data Source=:memory:"), settings);
		Invoking(() => connector.Command("select 1;").QuerySingle<int>()).Should().Throw<NotImplementedException>();
	}

	[Test]
	public void RetryOpenPolicy_EmptyResiliencePipeline()
	{
		var settings = new DbConnectorSettings { RetryOpenPolicy = PollyDbRetryPolicy.Create(ResiliencePipeline.Empty) };
		using var connector = new DbConnector(new SqliteConnection("Data Source=:memory:"), settings);
		connector.Command("select 1;").QuerySingle<int>().Should().Be(1);
	}

	private sealed class FakeDbRetryPolicy : DbRetryPolicy
	{
		public override void Execute(DbConnector connector, Action action) => throw new NotImplementedException();

		public override async ValueTask ExecuteAsync(DbConnector connector, Func<CancellationToken, ValueTask> action, CancellationToken cancellationToken = default) => throw new NotImplementedException();
	}
}
