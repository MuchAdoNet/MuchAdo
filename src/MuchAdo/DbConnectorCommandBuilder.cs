using System.Data;
using System.Text;
using MuchAdo.SqlFormatting;
using static System.FormattableString;

namespace MuchAdo;

internal sealed class DbConnectorCommandBuilder
{
	public DbConnectorCommandBuilder(SqlSyntax syntax)
	{
		Syntax = syntax;
		m_textBuilder = new StringBuilder();
		m_parametersList = new DbParametersList();
	}

	public SqlSyntax Syntax { get; }

	public string Text => m_textBuilder.ToString();

	public int TextLength => m_textBuilder.Length;

	public DbParameters Parameters => m_parametersList;

	public void AppendText(string text)
	{
		if (text.Length != 0)
		{
			ApplyPrefixes();
			m_textBuilder.Append(text);
		}
	}

	public void AppendText(char ch)
	{
		ApplyPrefixes();
		m_textBuilder.Append(ch);
	}

	public void AppendParameterValue<T>(object? key, T value)
	{
		ApplyPrefixes();

		if (key is null || m_parameterNames is null || !m_parameterNames.TryGetValue(key, out var name))
		{
			name = Invariant($"{Syntax.UnnamedParameterPrefix}{++m_parameterCount}");
			m_parametersList.Add(DbParameters.Create(name, value));
			if (key is not null)
				(m_parameterNames ??= new()).Add(key, name);
		}

		m_textBuilder.Append(Syntax.ParameterStart);
		m_textBuilder.Append(name);
	}

	public void AppendParameterValue<T>(object? key, T valueSource, DbDtoProperty<T> valueProperty)
	{
		ApplyPrefixes();

		if (key is null || m_parameterNames is null || !m_parameterNames.TryGetValue(key, out var name))
		{
			name = Invariant($"{Syntax.UnnamedParameterPrefix}{++m_parameterCount}");
			m_parametersList.Add(valueProperty.CreateParameter(name, valueSource));
			if (key is not null)
				(m_parameterNames ??= new()).Add(key, name);
		}

		m_textBuilder.Append(Syntax.ParameterStart);
		m_textBuilder.Append(name);
	}

	private void ApplyPrefixes()
	{
		if (m_prefixes is { Count: not 0 })
		{
			for (var index = 0; index < m_prefixes.Count; index++)
			{
				var prefix = m_prefixes[index];
				if (prefix is not null)
				{
					m_textBuilder.Append(prefix);
					m_prefixes[index] = null;
				}
			}
		}
	}

	public void AddParameters(DbParameters parameters) => m_parametersList.Add(parameters);

	public DbConnectorBracketScope Prefix(string prefix) => Bracket(prefix, "");

	public DbConnectorBracketScope Bracket(string prefix, string suffix)
	{
		(m_prefixes ??= new()).Add(prefix);
		(m_suffixes ??= new()).Add(suffix);
		return new(this);
	}

	internal void EndBracket()
	{
		var index = m_prefixes!.Count - 1;
		if (m_prefixes![index] is null)
			m_textBuilder.Append(m_suffixes![index]);
		m_prefixes!.RemoveAt(index);
		m_suffixes!.RemoveAt(index);
	}

	public DbConnectorCommand Build(DbConnector connector) => new DbConnectorCommand(connector, m_textBuilder.ToString(), CommandType.Text).WithParameters(m_parametersList);

	private readonly StringBuilder m_textBuilder;
	private readonly DbParametersList m_parametersList;
	private int m_parameterCount;
	private List<string?>? m_prefixes;
	private List<string?>? m_suffixes;
	private Dictionary<object, string>? m_parameterNames;
}
