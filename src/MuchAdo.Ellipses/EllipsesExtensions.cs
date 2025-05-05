using System.Collections;
using System.Text.RegularExpressions;

namespace MuchAdo.Ellipses;

/// <summary>
/// Methods for expanding ellipses in SQL.
/// </summary>
public static class EllipsesExtensions
{
	/// <summary>
	/// Expands ellipses in the specified command batch.
	/// </summary>
	public static DbConnectorCommandBatch ExpandEllipses(this DbConnectorCommandBatch commandBatch)
	{
		for (var commandIndex = 0; commandIndex < commandBatch.CommandCount; commandIndex++)
		{
			var command = commandBatch.GetCommand(commandIndex);
			var commandText = command.Text ?? command.Sql!.ToString(commandBatch.Connector.SqlSyntax);
			var parameters = command.Parameters;

			if (commandText.ContainsOrdinal("..."))
			{
				var parameterList = parameters.Enumerate().ToList();
				var parameterIndex = 0;
				while (parameterIndex < parameterList.Count)
				{
					// look for @name... in SQL for collection parameters
					var parameter = parameterList[parameterIndex];
					if (!string.IsNullOrEmpty(parameter.Name) && parameter.Value is not string && parameter.Value is not byte[] && parameter.Value is IEnumerable list)
					{
						var itemCount = -1;
						var replacements = new List<SqlParam<object?>>();

						string Replacement(Match match)
						{
							if (itemCount == -1)
							{
								itemCount = 0;

								foreach (var item in list)
								{
									replacements.Add(Sql.NamedParam<object?>($"{parameter.Name}_{itemCount}", item, parameter.Type));
									itemCount++;
								}

								if (itemCount == 0)
									throw new InvalidOperationException($"Collection parameter '{parameter.Name}' must not be empty.");
							}

							return string.Join(",", Enumerable.Range(0, itemCount).Select(x => $"{match.Groups[1]}_{x}"));
						}

						commandText = Regex.Replace(commandText, $@"([?@:]{Regex.Escape(parameter.Name)})\.\.\.",
							Replacement, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

						// if special syntax wasn't found, leave the parameter alone, for databases that support collections directly
						if (itemCount != -1)
						{
							parameters = new SqlParamSources(parameterList.Take(parameterIndex).Concat(replacements).Concat(parameterList.Skip(parameterIndex + 1)));
							parameterIndex += replacements.Count;
						}
						else
						{
							parameterIndex += 1;
						}
					}
					else
					{
						parameterIndex += 1;
					}
				}

				commandBatch.SetCommand(commandIndex, new(command.Type, commandText, parameters));
			}
		}

		return commandBatch;
	}
}
