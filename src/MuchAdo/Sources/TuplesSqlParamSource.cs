namespace MuchAdo.Sources;

internal sealed class TuplesSqlParamSource<T>(IEnumerable<(string Name, T Value)> tuples) : SqlParamSource
{
	internal override void SubmitParameters(ISqlParamTarget target)
	{
		foreach (var tuple in m_tuples)
			target.AcceptParameter(tuple.Name, tuple.Value, type: null);
	}

	private readonly IEnumerable<(string Name, T Value)> m_tuples = tuples.Memoize();
}
