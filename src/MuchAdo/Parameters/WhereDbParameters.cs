namespace MuchAdo.Parameters;

internal sealed class WhereDbParameters(DbParameters source, Func<string, bool> where) : DbParameters
{
	internal override int CountCore(Func<string, bool>? filterName, Func<string, string>? transformName) =>
		source.CountCore(FilterName(filterName, transformName), transformName);

	internal override IEnumerable<(string Name, object? Value)> EnumerateCore(Func<string, bool>? filterName, Func<string, string>? transformName) =>
		source.EnumerateCore(FilterName(filterName, transformName), transformName);

	internal override void ApplyCore(DbConnector connector, Func<string, bool>? filterName, Func<string, string>? transformName) =>
		source.ApplyCore(connector, FilterName(filterName, transformName), transformName);

	internal override int ReapplyCore(DbConnector connector, int startIndex, Func<string, bool>? filterName, Func<string, string>? transformName) =>
		source.ReapplyCore(connector, startIndex, FilterName(filterName, transformName), transformName);

	private Func<string, bool> FilterName(Func<string, bool>? filterName, Func<string, string>? transformName) =>
		filterName is null ? where : x => where(x) && filterName(transformName is null ? x : transformName(x));
}
