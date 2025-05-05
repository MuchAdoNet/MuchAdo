namespace MuchAdo.Sources;

internal sealed class RenamedSqlParamSource(SqlParamSource source, Func<string, string> rename) : SqlParamSource
{
	internal override void SubmitParameters(ISqlParamTarget target) => source.SubmitParameters(new RenamedSqlParamTarget(target, rename));

	private sealed class RenamedSqlParamTarget(ISqlParamTarget target, Func<string, string> rename) : ISqlParamTarget
	{
		public void AcceptParameter<T>(string name, T value, SqlParamType? type) => target.AcceptParameter(rename(name), value, type);
	}
}
