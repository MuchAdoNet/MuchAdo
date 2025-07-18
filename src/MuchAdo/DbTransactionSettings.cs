using System.Data;

namespace MuchAdo;

/// <summary>
/// Settings for a database transaction.
/// </summary>
public class DbTransactionSettings
{
	/// <summary>
	/// The default transaction settings, which do not specify an isolation level.
	/// </summary>
	public static DbTransactionSettings Default { get; } = new(null);

	/// <summary>
	/// Creates transaction settings with the specified isolation level.
	/// </summary>
	public static DbTransactionSettings FromIsolationLevel(IsolationLevel isolationLevel) => new(isolationLevel);

	/// <summary>
	/// Implicitly casts an isolation level to transaction settings with that isolation level.
	/// </summary>
	public static implicit operator DbTransactionSettings(IsolationLevel isolationLevel) => FromIsolationLevel(isolationLevel);

	/// <summary>
	/// The isolation level for the transaction, or <c>null</c> if not specified.
	/// </summary>
	public IsolationLevel? IsolationLevel { get; }

	/// <summary>
	/// Creates transaction settings with the specified isolation level.
	/// </summary>
	protected DbTransactionSettings(IsolationLevel? isolationLevel)
	{
		IsolationLevel = isolationLevel;
	}
}
