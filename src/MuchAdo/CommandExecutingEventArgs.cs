namespace MuchAdo;

public sealed class CommandExecutingEventArgs(DbConnectorCommand connectorCommand) : EventArgs
{
	public DbConnectorCommand ConnectorCommand { get; } = connectorCommand;
}
