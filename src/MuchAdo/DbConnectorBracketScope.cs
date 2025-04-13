namespace MuchAdo;

internal readonly struct DbConnectorBracketScope(DbConnectorCommandBuilder commandBuilder) : IDisposable
{
	public void Dispose() => commandBuilder.EndBracket();
}
