namespace MuchAdo.Sources;

internal sealed class EmptySqlParamSource : SqlParamSource
{
	internal override void Render(DbConnectorCommandBuilder builder)
	{
	}

	internal override void SubmitParameters(ISqlParamTarget target)
	{
	}
}
