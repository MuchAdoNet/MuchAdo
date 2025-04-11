using System.Data;

namespace MuchAdo.Parameters;

internal sealed class SingleDbParameter<T>(string name, T value) : DbParameters
{
	internal override int CountCore(Func<string, bool>? filterName, Func<string, string>? transformName) => filterName is null || filterName(name) ? 1 : 0;

	internal override IEnumerable<(string Name, object? Value)> EnumerateCore(Func<string, bool>? filterName, Func<string, string>? transformName)
	{
		if (filterName is null || filterName(name))
		{
			var transformedName = transformName is null ? name : transformName(name);
			yield return (transformedName, value);
		}
	}

	internal override void ApplyCore(DbConnector connector, Func<string, bool>? filterName, Func<string, string>? transformName)
	{
		if (filterName is null || filterName(name))
		{
			var transformedName = transformName is null ? name : transformName(name);
			if (value is IDataParameter dbParameter)
				dbParameter.ParameterName = transformedName;
			else
				dbParameter = connector.CreateParameter(transformedName, value);

			connector.ActiveCommand.Parameters.Add(dbParameter);
		}
	}

	internal override int ReapplyCore(DbConnector connector, int startIndex, Func<string, bool>? filterName, Func<string, string>? transformName)
	{
		if (filterName is null || filterName(name))
		{
			var command = connector.ActiveCommand;
			var transformedName = transformName is null ? name : transformName(name);
			var dbParameter = command.Parameters[startIndex] as IDataParameter;
			if (dbParameter is null || dbParameter.ParameterName != transformedName)
			{
				try
				{
					dbParameter = command.Parameters[transformedName] as IDataParameter;
				}
				catch (Exception exception)
				{
					throw new InvalidOperationException($"Cached commands must always be executed with the same parameters (missing '{transformedName}').", exception);
				}
				if (dbParameter is null)
					throw new InvalidOperationException($"Cached commands must always be executed with the same parameters (missing '{transformedName}').");
			}

			connector.SetParameterValue(dbParameter, value);
			return 1;
		}

		return 0;
	}
}
