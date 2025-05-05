namespace MuchAdo.Sources;

internal sealed class FilteredSqlParamSource(SqlParamSource source, Func<string, bool> nameMatches) : SqlParamSource
{
	internal override void SubmitParameters(ISqlParamTarget target) => source.SubmitParameters(new FilteredSqlParamTarget(target, nameMatches));

	private sealed class FilteredSqlParamTarget(ISqlParamTarget target, Func<string, bool> where) : ISqlParamTarget
	{
		public void AcceptParameter<T>(string name, T value, SqlParamType? type)
		{
			if (where(name))
				target.AcceptParameter(name, value, type);
		}
	}
}
