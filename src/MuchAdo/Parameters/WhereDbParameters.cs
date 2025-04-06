using System.Data;

namespace MuchAdo.Parameters;

internal sealed class WhereDbParameters(DbParameters source, Func<string, bool> where) : DbParameters
{
	internal override int CountCore(Func<string, bool>? filterName, Func<string, string>? transformName) =>
		source.CountCore(FilterName(filterName, transformName), transformName);

	internal override IEnumerable<(string Name, object? Value)> EnumerateCore(Func<string, bool>? filterName, Func<string, string>? transformName) =>
		source.EnumerateCore(FilterName(filterName, transformName), transformName);

	internal override void ApplyCore(IDbCommand command, DbProviderMethods providerMethods, Func<string, bool>? filterName, Func<string, string>? transformName) =>
		source.ApplyCore(command, providerMethods, FilterName(filterName, transformName), transformName);

	internal override int ReapplyCore(IDbCommand command, int startIndex, DbProviderMethods providerMethods, Func<string, bool>? filterName, Func<string, string>? transformName) =>
		source.ReapplyCore(command, startIndex, providerMethods, FilterName(filterName, transformName), transformName);

	private Func<string, bool> FilterName(Func<string, bool>? filterName, Func<string, string>? transformName) =>
		filterName is null ? where : x => where(x) && filterName(transformName is null ? x : transformName(x));
}
