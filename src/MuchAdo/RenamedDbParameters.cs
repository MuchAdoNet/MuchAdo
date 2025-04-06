using System.Data;

namespace MuchAdo;

internal sealed class RenamedDbParameters(DbParameters source, Func<string, string> named) : DbParameters
{
	internal override int CountCore(Func<string, bool>? filterName, Func<string, string>? transformName) =>
		source.CountCore(FilterName(filterName), TransformName(transformName));

	internal override IEnumerable<(string Name, object? Value)> EnumerateCore(Func<string, bool>? filterName, Func<string, string>? transformName) =>
		source.EnumerateCore(FilterName(filterName), TransformName(transformName));

	internal override void ApplyCore(IDbCommand command, DbProviderMethods providerMethods, Func<string, bool>? filterName, Func<string, string>? transformName) =>
		source.ApplyCore(command, providerMethods, FilterName(filterName), TransformName(transformName));

	internal override int ReapplyCore(IDbCommand command, int startIndex, DbProviderMethods providerMethods, Func<string, bool>? filterName, Func<string, string>? transformName) =>
		source.ReapplyCore(command, startIndex, providerMethods, FilterName(filterName), TransformName(transformName));

	private Func<string, bool>? FilterName(Func<string, bool>? filterName) =>
		filterName is null ? null : x => filterName(named(x));

	private Func<string, string> TransformName(Func<string, string>? transformName) =>
		transformName is null ? named : x => transformName(named(x));
}
