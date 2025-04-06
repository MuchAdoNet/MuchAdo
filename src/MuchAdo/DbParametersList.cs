using System.Data;

namespace MuchAdo;

/// <summary>
/// A list of sets of parameters.
/// </summary>
public sealed class DbParametersList : DbParameters
{
	/// <summary>
	/// Creates an empty list.
	/// </summary>
	public DbParametersList() => m_parametersList = [];

	/// <summary>
	/// Creates a list from the specified sets of parameters.
	/// </summary>
	public DbParametersList(params IEnumerable<DbParameters> items) => m_parametersList = [.. items];

	public bool IsReadOnly
	{
		get => m_isReadOnly;
		set
		{
			VerifyNotReadOnly();
			m_isReadOnly = value;
		}
	}

	public void Add(DbParameters item)
	{
		VerifyNotReadOnly();
		m_parametersList.Add(item);
	}

	internal override int CountCore(Func<string, bool>? filterName, Func<string, string>? transformName) =>
		m_parametersList.Sum(x => x.CountCore(filterName, transformName));

	internal override IEnumerable<(string Name, object? Value)> EnumerateCore(Func<string, bool>? filterName, Func<string, string>? transformName) =>
		m_parametersList.SelectMany(x => x.EnumerateCore(filterName, transformName));

	internal override void ApplyCore(IDbCommand command, DbProviderMethods providerMethods, Func<string, bool>? filterName, Func<string, string>? transformName)
	{
		m_isReadOnly = true;
		foreach (var parameters in m_parametersList)
			parameters.ApplyCore(command, providerMethods, filterName, transformName);
	}

	internal override int ReapplyCore(IDbCommand command, int startIndex, DbProviderMethods providerMethods, Func<string, bool>? filterName, Func<string, string>? transformName)
	{
		m_isReadOnly = true;
		var parameterCount = 0;
		foreach (var parameters in m_parametersList)
			parameterCount += parameters.ReapplyCore(command, startIndex + parameterCount, providerMethods, filterName, transformName);
		return parameterCount;
	}

	private void VerifyNotReadOnly()
	{
		if (m_isReadOnly)
			throw new NotSupportedException("This instance is read-only.");
	}

	private readonly List<DbParameters> m_parametersList;
	private bool m_isReadOnly;
}
