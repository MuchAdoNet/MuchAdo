using System.Globalization;
using System.Text;
using static System.FormattableString;

namespace MuchAdo;

internal sealed class DbConnectorCommandBuilder
{
	public DbConnectorCommandBuilder(SqlSyntax syntax, bool buildText, ISqlParamTarget? paramTarget)
	{
		Syntax = syntax;
		m_textBuilder = buildText ? new StringBuilder(capacity: 128) : null;
		m_paramTarget = paramTarget;
	}

	public SqlSyntax Syntax { get; }

	public int TextLength => m_textLength;

	public string GetText() => m_textBuilder?.ToString() ?? "";

	public void AppendText(string text)
	{
		if (text.Length != 0)
		{
			ApplyPrefixes();
			m_textBuilder?.Append(text);
			m_textLength += text.Length;
		}
	}

	public void SubmitParameters(SqlParamSource parameters)
	{
		if (m_paramTarget is not null)
			parameters.SubmitParameters(m_paramTarget);
	}

	public void AppendParameterValue<T>(T value, SqlParamType? type = null, object? identity = null)
	{
		ApplyPrefixes();

		string? needsParameterNamed;
		if (identity is null || m_placeholders is null || !m_placeholders.TryGetValue(identity, out var tuple))
		{
			if (Syntax.UnnamedParameterStrategy.NamedParameterNamePrefix is { } namedPrefix)
			{
				needsParameterNamed = Invariant($"{namedPrefix}{++m_parameterCount}");
				tuple = (Syntax.NamedParameterPrefix, needsParameterNamed, -1);
				tuple.Name = needsParameterNamed;
				if (identity is not null)
					(m_placeholders ??= new()).Add(identity, tuple);
			}
			else if (Syntax.UnnamedParameterStrategy.NumberedParameterPlaceholderPrefix is { } numberedPrefix)
			{
				needsParameterNamed = "";
				tuple = (numberedPrefix, "", ++m_parameterCount);
				if (identity is not null)
					(m_placeholders ??= new()).Add(identity, tuple);
			}
			else if (Syntax.UnnamedParameterStrategy.UnnumberedParameterPlaceholder is { } unnumberedPlaceholder)
			{
				needsParameterNamed = "";
				tuple = (unnumberedPlaceholder, "", -1);
			}
			else
			{
				throw new InvalidOperationException($"Unexpected {nameof(Syntax.UnnamedParameterStrategy)}.");
			}
		}
		else
		{
			needsParameterNamed = null;
		}

		m_textBuilder?.Append(tuple.Prefix);
		m_textLength += tuple.Prefix.Length;

		m_textBuilder?.Append(tuple.Name);
		m_textLength += tuple.Name.Length;

		if (tuple.Number >= 0)
		{
#if NET
			Span<char> numberBuffer = stackalloc char[10];
			tuple.Number.TryFormat(numberBuffer, out var numberLength, provider: CultureInfo.InvariantCulture);
			m_textBuilder?.Append(numberBuffer[..numberLength]);
			m_textLength += numberLength;
#else
			var numberString = tuple.Number.ToString(CultureInfo.InvariantCulture);
			m_textBuilder?.Append(numberString);
			m_textLength += numberString.Length;
#endif
		}

		if (m_paramTarget is not null && needsParameterNamed is not null)
			m_paramTarget.AcceptParameter(needsParameterNamed, value, type);
	}

	private void ApplyPrefixes()
	{
		if (m_brackets is { Count: not 0 })
		{
			for (var index = 0; index < m_brackets.Count; index++)
			{
				var (prefix, suffix) = m_brackets[index];
				if (prefix is not null)
				{
					m_textBuilder?.Append(prefix);
					m_textLength += prefix.Length;
					m_brackets[index] = (null, suffix);
				}
			}
		}
	}

	public DbConnectorBracketScope Prefix(string prefix) => Bracket(prefix, null);

	public DbConnectorBracketScope Bracket(string prefix, string? suffix)
	{
		(m_brackets ??= new()).Add((prefix, suffix));
		return new(this);
	}

	internal void EndBracket()
	{
		var index = m_brackets!.Count - 1;
		var (prefix, suffix) = m_brackets[index];
		if (prefix is null && suffix is not null)
		{
			m_textBuilder?.Append(suffix);
			m_textLength += suffix.Length;
		}
		m_brackets!.RemoveAt(index);
	}

	private readonly StringBuilder? m_textBuilder;
	private readonly ISqlParamTarget? m_paramTarget;
	private int m_textLength;
	private int m_parameterCount;
	private List<(string? Prefix, string? Suffix)>? m_brackets;
	private Dictionary<object, (string Prefix, string Name, int Number)>? m_placeholders;
}
