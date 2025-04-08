using System.Diagnostics.CodeAnalysis;
using MuchAdo.Parameters;

namespace MuchAdo;

/// <summary>
/// A set of database parameters.
/// </summary>
public abstract class DbParameters
{
	/// <summary>
	/// The number of parameters.
	/// </summary>
	public int Count => CountCore(filterName: null, transformName: null);

	/// <summary>
	/// Enumerates the names and values of the parameters.
	/// </summary>
	public IEnumerable<(string Name, object? Value)> Enumerate() => EnumerateCore(filterName: null, transformName: null);

	/// <summary>
	/// An empty list of parameters.
	/// </summary>
	[SuppressMessage("Performance", "CA1805:Do not initialize unnecessarily", Justification = "Intentional API.")]
	public static readonly DbParameters Empty = new EmptyDbParameters();

	/// <summary>
	/// Creates one parameter.
	/// </summary>
	public static DbParameters Create<T>(string name, T value) =>
		new SingleDbParameter<T>(name, value);

	/// <summary>
	/// Creates parameters from a sequence of parameters.
	/// </summary>
	public static DbParameters Create(params IEnumerable<DbParameters> parameters) =>
		new DbParametersList(parameters ?? throw new ArgumentNullException(nameof(parameters))) { IsReadOnly = true };

	/// <summary>
	/// Creates parameters from a sequence of name/value pairs.
	/// </summary>
	public static DbParameters Create<T>(params IEnumerable<(string Name, T Value)> parameters) =>
		Create((parameters ?? throw new ArgumentNullException(nameof(parameters))).Select(x => Create(x.Name, x.Value)));

	/// <summary>
	/// Creates parameters from a dictionary.
	/// </summary>
	public static DbParameters Create<T>(IEnumerable<KeyValuePair<string, T>> parameters) =>
		Create((parameters ?? throw new ArgumentNullException(nameof(parameters))).Select(x => Create(x.Key, x.Value)));

	/// <summary>
	/// Creates a list of parameters from the properties of a DTO.
	/// </summary>
	/// <remarks>The name of each parameter is the name of the corresponding DTO property.</remarks>
	public static DbParameters FromDto<T>(T dto)
	{
		if (dto is null)
			throw new ArgumentNullException(nameof(dto));
		return new DtoDbParameters<T>(dto);
	}

	/// <summary>
	/// Filters the parameters by name.
	/// </summary>
	public DbParameters Where(Func<string, bool> nameMatches)
	{
		if (nameMatches is null)
			throw new ArgumentNullException(nameof(nameMatches));
		return new WhereDbParameters(this, nameMatches);
	}

	/// <summary>
	/// Transforms the parameter names using the specified function.
	/// </summary>
	public DbParameters Renamed(Func<string, string> transform)
	{
		if (transform is null)
			throw new ArgumentNullException(nameof(transform));
		return new RenamedDbParameters(this, transform);
	}

	internal void Apply(DbConnector connector) =>
		ApplyCore(connector, filterName: null, transformName: null);

	internal int Reapply(DbConnector connector, int startIndex) =>
		ReapplyCore(connector, startIndex, filterName: null, transformName: null);

	internal abstract int CountCore(Func<string, bool>? filterName, Func<string, string>? transformName);

	internal abstract IEnumerable<(string Name, object? Value)> EnumerateCore(Func<string, bool>? filterName, Func<string, string>? transformName);

	internal abstract void ApplyCore(DbConnector connector, Func<string, bool>? filterName, Func<string, string>? transformName);

	internal abstract int ReapplyCore(DbConnector connector, int startIndex, Func<string, bool>? filterName, Func<string, string>? transformName);

	private sealed class EmptyDbParameters : DbParameters
	{
		internal override int CountCore(Func<string, bool>? filterName, Func<string, string>? transformName) => 0;

		internal override IEnumerable<(string Name, object? Value)> EnumerateCore(Func<string, bool>? filterName, Func<string, string>? transformName) => [];

		internal override void ApplyCore(DbConnector connector, Func<string, bool>? filterName, Func<string, string>? transformName)
		{
		}

		internal override int ReapplyCore(DbConnector connector, int startIndex, Func<string, bool>? filterName, Func<string, string>? transformName) => 0;
	}
}
