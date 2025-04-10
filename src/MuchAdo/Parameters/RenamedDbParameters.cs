namespace MuchAdo.Parameters;

internal sealed class RenamedDbParameters(DbParameters source, Func<string, string> named) : DbParameters
{
	internal override int CountCore(Func<string, bool>? filterName, Func<string, string>? transformName) =>
		source.CountCore(FilterName(filterName), TransformName(transformName));

	internal override IEnumerable<(string Name, object? Value)> EnumerateCore(Func<string, bool>? filterName, Func<string, string>? transformName) =>
		source.EnumerateCore(FilterName(filterName), TransformName(transformName));

	internal override void ApplyCore(DbConnector connector, Func<string, bool>? filterName, Func<string, string>? transformName) =>
		source.ApplyCore(connector, FilterName(filterName), TransformName(transformName));

	internal override int ReapplyCore(DbConnector connector, int startIndex, Func<string, bool>? filterName, Func<string, string>? transformName) =>
		source.ReapplyCore(connector, startIndex, FilterName(filterName), TransformName(transformName));

	private Func<string, bool>? FilterName(Func<string, bool>? filterName) =>
		filterName is null ? null : x => filterName(named(x));

	private Func<string, string> TransformName(Func<string, string>? transformName) =>
		transformName is null ? named : x => transformName(named(x));
}
