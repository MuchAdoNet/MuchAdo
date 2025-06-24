using Polly;

namespace MuchAdo.Polly;

/// <summary>
/// A Polly-based implementation of <see cref="DbRetryPolicy"/> for database retry operations.
/// </summary>
public abstract class PollyDbRetryPolicy : DbRetryPolicy
{
	/// <summary>
	/// Creates a new instance of <see cref="PollyDbRetryPolicy"/> using the specified resilience pipeline.
	/// </summary>
	public static PollyDbRetryPolicy Create(ResiliencePipeline resiliencePipeline) => new ResiliencePipelineDbRetryPolicy(resiliencePipeline);
}
