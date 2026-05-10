namespace MuchAdo.Sources;

internal sealed class CombineSqlParamSource(IEnumerable<SqlParamSource> sources) : SqlParamSource
{
	internal override void SubmitParameters(ISqlParamTarget target)
	{
		foreach (var source in m_sources)
			source.SubmitParameters(target);
	}

	private readonly IEnumerable<SqlParamSource> m_sources = sources.Memoize();
}
