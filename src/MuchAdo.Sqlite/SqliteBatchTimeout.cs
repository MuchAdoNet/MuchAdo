using System.Diagnostics;
using Microsoft.Data.Sqlite;

namespace MuchAdo.Sqlite;

/// <summary>
/// Encapsulates timeout tracking logic for batch command execution.
/// </summary>
/// <remarks>
/// When executing multiple commands in a batch, the overall timeout should be distributed across
/// all commands. This type tracks elapsed time and calculates the remaining timeout for each
/// successive command, ensuring the total execution time respects the original timeout setting.
/// </remarks>
internal struct SqliteBatchTimeout
{
	/// <summary>
	/// Creates a new batch timeout tracker.
	/// </summary>
	/// <param name="timeout">The initial timeout in seconds, or null if no timeout is set.</param>
	public SqliteBatchTimeout(int? timeout)
	{
		m_timeoutRemaining = timeout;

		if (timeout is not null)
			m_stopwatch = Stopwatch.StartNew();
	}

	/// <summary>
	/// Applies the current remaining timeout to the command before execution.
	/// </summary>
	/// <param name="command">The command to configure.</param>
	public readonly void ApplyTimeout(SqliteCommand command)
	{
		if (m_timeoutRemaining is not null)
			command.CommandTimeout = m_timeoutRemaining.Value;
	}

	/// <summary>
	/// Updates the remaining timeout after a command has executed.
	/// </summary>
	/// <remarks>
	/// Call this after each command execution to recalculate the remaining time.
	/// The remaining timeout will never drop below 1 second (to avoid zero which means infinite).
	/// </remarks>
	public void UpdateAfterExecution()
	{
		if (m_timeoutRemaining > 0)
			m_timeoutRemaining = Math.Max(1, m_timeoutRemaining.Value - (int) (m_stopwatch!.ElapsedMilliseconds / 1000));
	}

	private readonly Stopwatch? m_stopwatch;
	private int? m_timeoutRemaining;
}
