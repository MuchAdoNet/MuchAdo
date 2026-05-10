namespace MuchAdo.Sources;

internal sealed class ParamsSqlParamSource<T>(IEnumerable<T> items) : SqlParamSource
{
	internal override void SubmitParameters(ISqlParamTarget target)
	{
		foreach (var item in m_items)
		{
			if (item is SqlParamSource paramSource)
				paramSource.SubmitParameters(target);
			else
				target.AcceptParameter("", item, type: null);
		}
	}

	private readonly IEnumerable<T> m_items = items.Memoize();
}
