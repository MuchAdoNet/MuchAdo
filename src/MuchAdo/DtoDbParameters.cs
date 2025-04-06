using System.Data;

namespace MuchAdo;

internal sealed class DtoDbParameters<T>(T dto) : DbParameters
{
	internal override int CountCore(Func<string, bool>? filterName, Func<string, string>? transformName) =>
		filterName is null
			? DbDtoInfo.GetInfo<T>().Properties.Count
			: DbDtoInfo.GetInfo<T>().Properties.Count(x => filterName(x.Name));

	internal override IEnumerable<(string Name, object? Value)> EnumerateCore(Func<string, bool>? filterName, Func<string, string>? transformName) =>
			EnumerateParameters(filterName, transformName).SelectMany(x => x.Enumerate());

	internal override void ApplyCore(IDbCommand command, DbProviderMethods providerMethods, Func<string, bool>? filterName, Func<string, string>? transformName)
	{
		foreach (var parameters in EnumerateParameters(filterName, transformName))
			parameters.Apply(command, providerMethods);
	}

	internal override int ReapplyCore(IDbCommand command, int startIndex, DbProviderMethods providerMethods, Func<string, bool>? filterName, Func<string, string>? transformName)
	{
		var parameterCount = 0;
		foreach (var parameters in EnumerateParameters(filterName, transformName))
			parameterCount += parameters.Reapply(command, startIndex + parameterCount, providerMethods);
		return parameterCount;
	}

	private IEnumerable<DbParameters> EnumerateParameters(Func<string, bool>? filterName, Func<string, string>? transformName)
	{
		foreach (var property in DbDtoInfo.GetInfo<T>().Properties)
		{
			var name = property.Name;
			if (filterName is null || filterName(name))
			{
				var transformedName = transformName is null ? name : transformName(name);
				yield return property.CreateParameter(transformedName, dto);
			}
		}
	}
}
