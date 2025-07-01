using Polly;

namespace MuchAdo.Polly;

internal sealed class ResiliencePipelineDbRetryPolicy(ResiliencePipeline resiliencePipeline) : PollyDbRetryPolicy
{
	public override void Execute(DbConnector connector, Action action) =>
		resiliencePipeline.Execute(action);

	public override ValueTask ExecuteAsync(DbConnector connector, Func<CancellationToken, ValueTask> action, CancellationToken cancellationToken = default) =>
		resiliencePipeline.ExecuteAsync(action, cancellationToken);
}
