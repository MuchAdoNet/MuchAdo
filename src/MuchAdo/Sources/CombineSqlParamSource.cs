namespace MuchAdo.Sources;

internal sealed class CombineSqlParamSource(IEnumerable<SqlParamSource> sources) : SqlParamSource
{
	internal override void SubmitParameters(ISqlParamTarget target)
	{
		foreach (var source in sources)
			source.SubmitParameters(target);
	}
}
