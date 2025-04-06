using System.Data;
using System.Diagnostics.CodeAnalysis;

namespace MuchAdo;

/// <summary>
/// A cache of <see cref="IDbCommand" /> by command text.
/// </summary>
internal sealed class DbCommandCache
{
	public bool TryGetCommand(string text, [MaybeNullWhen(false)] out IDbCommand command) => m_dictionary.TryGetValue(text, out command);

	public void AddCommand(string text, IDbCommand command) => m_dictionary.Add(text, command);

	public IReadOnlyCollection<IDbCommand> GetCommands() => m_dictionary.Values;

	private readonly Dictionary<string, IDbCommand> m_dictionary = new();
}
