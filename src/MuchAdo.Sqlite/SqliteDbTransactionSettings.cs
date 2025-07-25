using System.Data;

namespace MuchAdo.Sqlite;

/// <summary>
/// Settings for a database transaction.
/// </summary>
public class SqliteDbTransactionSettings : DbTransactionSettings
{
	/// <summary>
	/// Transaction settings for a deferred transaction.
	/// </summary>
	public static SqliteDbTransactionSettings Deferred { get; } = new(isolationLevel: null, isDeferred: true);

	/// <summary>
	/// Creates transaction settings.
	/// </summary>
	public SqliteDbTransactionSettings(IsolationLevel? isolationLevel, bool isDeferred)
		: base(isolationLevel)
	{
		IsDeferred = isDeferred;
	}

	/// <summary>
	/// True if the transaction is deferred, meaning that it will not lock the database until a write operation is performed.
	/// </summary>
	public bool IsDeferred { get; }
}
