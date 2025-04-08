namespace MuchAdo.Parameters;

internal sealed class DtoDbParameters<T>(T dto) : DbParameters
{
	internal override int CountCore(Func<string, bool>? filterName, Func<string, string>? transformName) =>
		filterName is null
			? DbDtoInfo.GetInfo<T>().Properties.Count
			: DbDtoInfo.GetInfo<T>().Properties.Count(x => filterName(x.Name));

	internal override IEnumerable<(string Name, object? Value)> EnumerateCore(Func<string, bool>? filterName, Func<string, string>? transformName) =>
			EnumerateParameters(filterName, transformName).SelectMany(x => x.Enumerate());

	internal override void ApplyCore(DbConnector connector, Func<string, bool>? filterName, Func<string, string>? transformName)
	{
		foreach (var parameters in EnumerateParameters(filterName, transformName))
			parameters.Apply(connector);
	}

	internal override int ReapplyCore(DbConnector connector, int startIndex, Func<string, bool>? filterName, Func<string, string>? transformName)
	{
		var parameterCount = 0;
		foreach (var parameters in EnumerateParameters(filterName, transformName))
			parameterCount += parameters.Reapply(connector, startIndex + parameterCount);
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
