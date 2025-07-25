using Polly;

namespace MuchAdo.Polly;

internal sealed class ResiliencePipelineDbRetryPolicy(ResiliencePipeline resiliencePipeline) : PollyDbRetryPolicy
{
	protected override void ExecuteCore(DbConnector connector, Action action) =>
		resiliencePipeline.Execute(action);

	protected override ValueTask ExecuteCoreAsync(DbConnector connector, Func<CancellationToken, ValueTask> action, CancellationToken cancellationToken = default) =>
		resiliencePipeline.ExecuteAsync(action, cancellationToken);
}
