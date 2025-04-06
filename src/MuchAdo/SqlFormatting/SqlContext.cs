using static System.FormattableString;

namespace MuchAdo.SqlFormatting;

internal sealed class SqlContext
{
	public SqlContext(SqlSyntax syntax)
	{
		Syntax = syntax;
	}

	public SqlSyntax Syntax { get; }

	public DbParameters Parameters => m_parametersList ?? DbParameters.Empty;

	public void AddParameters(DbParameters parameters) => (m_parametersList ??= new()).Add(parameters);

	public string RenderParameter<T>(object? key, T value)
	{
		if (key is not null && m_renderedParams is not null && m_renderedParams.TryGetValue(key, out var rendered))
			return rendered;

		m_parametersList ??= new();
		var name = Invariant($"{Syntax.UnnamedParameterPrefix}{m_parametersList.Count}");
		m_parametersList.Add(DbParameters.Create(name, value));
		rendered = Syntax.ParameterStart + name;

		if (key is not null)
		{
			m_renderedParams ??= new();
			m_renderedParams.Add(key, rendered);
		}

		return rendered;
	}

	public string RenderParameter<T>(object? key, T valueSource, DbDtoProperty<T> valueProperty)
	{
		if (key is not null && m_renderedParams is not null && m_renderedParams.TryGetValue(key, out var rendered))
			return rendered;

		m_parametersList ??= new();
		var name = Invariant($"{Syntax.UnnamedParameterPrefix}{m_parametersList.Count}");
		m_parametersList.Add(valueProperty.CreateParameter(name, valueSource));
		rendered = Syntax.ParameterStart + name;

		if (key is not null)
		{
			m_renderedParams ??= new();
			m_renderedParams.Add(key, rendered);
		}

		return rendered;
	}

	private DbParametersList? m_parametersList;
	private Dictionary<object, string>? m_renderedParams;
}
