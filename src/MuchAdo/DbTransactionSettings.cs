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
	/// Transaction settings with <c>IsolationLevel.ReadUncommitted</c>.
	/// </summary>
	public static DbTransactionSettings ReadUncommitted { get; } = new(System.Data.IsolationLevel.ReadUncommitted);

	/// <summary>
	/// Transaction settings with <c>IsolationLevel.ReadCommitted</c>.
	/// </summary>
	public static DbTransactionSettings ReadCommitted { get; } = new(System.Data.IsolationLevel.ReadCommitted);

	/// <summary>
	/// Transaction settings with <c>IsolationLevel.RepeatableRead</c>.
	/// </summary>
	public static DbTransactionSettings RepeatableRead { get; } = new(System.Data.IsolationLevel.RepeatableRead);

	/// <summary>
	/// Transaction settings with <c>IsolationLevel.Serializable</c>.
	/// </summary>
	public static DbTransactionSettings Serializable { get; } = new(System.Data.IsolationLevel.Serializable);

	/// <summary>
	/// Creates transaction settings with the specified isolation level.
	/// </summary>
	public static DbTransactionSettings FromIsolationLevel(IsolationLevel isolationLevel) =>
		isolationLevel switch
		{
			System.Data.IsolationLevel.ReadUncommitted => ReadUncommitted,
			System.Data.IsolationLevel.ReadCommitted => ReadCommitted,
			System.Data.IsolationLevel.RepeatableRead => RepeatableRead,
			System.Data.IsolationLevel.Serializable => Serializable,
			_ => new DbTransactionSettings(isolationLevel),
		};

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
