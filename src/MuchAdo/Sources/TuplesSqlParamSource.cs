namespace MuchAdo.Sources;

internal sealed class TuplesSqlParamSource<T>(IEnumerable<(string Name, T Value)> tuples) : SqlParamSource
{
	internal override void SubmitParameters(ISqlParamTarget target)
	{
		foreach (var tuple in tuples)
			target.AcceptParameter(tuple.Name, tuple.Value, type: null);
	}
}
