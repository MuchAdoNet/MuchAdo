namespace MuchAdo;

/// <summary>
/// Settings when creating a <see cref="DbConnectorPool"/>.
/// </summary>
public class DbConnectorPoolSettings
{
	/// <summary>
	/// Creates a new connector for the pool.
	/// </summary>
	/// <remarks>The created connector is wrapped in a connector that stores the actual connector
	/// in the pool when disposed. Since the advantage of a connector pool is keeping database
	/// connections open, avoid using <see cref="DbConnector.CloseConnection" /> or
	/// <see cref="DbConnector.CloseConnectionAsync" />.</remarks>
	public Func<DbConnector>? CreateConnector { get; init; }
}
