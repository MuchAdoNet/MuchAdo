using System.Collections;
using System.Text.RegularExpressions;

namespace MuchAdo.Ellipses;

/// <summary>
/// Methods for expanding ellipses in SQL.
/// </summary>
public static class EllipsesExtensions
{
	public static DbConnectorCommand ExpandEllipses(this DbConnectorCommand connectorCommand)
	{
		var commandText = connectorCommand.Text;
		var parameters = connectorCommand.Parameters;

		if (commandText.ContainsOrdinal("..."))
		{
			var nameValuePairs = parameters.Enumerate().ToList();
			var index = 0;
			while (index < nameValuePairs.Count)
			{
				// look for @name... in SQL for collection parameters
				var (name, value) = nameValuePairs[index];
				if (!string.IsNullOrEmpty(name) && value is not string && value is not byte[] && value is IEnumerable list)
				{
					var itemCount = -1;
					var replacements = new List<(string Name, object? Value)>();

					string Replacement(Match match)
					{
						if (itemCount == -1)
						{
							itemCount = 0;

							foreach (var item in list)
							{
								replacements.Add(($"{name}_{itemCount}", item));
								itemCount++;
							}

							if (itemCount == 0)
								throw new InvalidOperationException($"Collection parameter '{name}' must not be empty.");
						}

						return string.Join(",", Enumerable.Range(0, itemCount).Select(x => $"{match.Groups[1]}_{x}"));
					}

					commandText = Regex.Replace(commandText, $@"([?@:]{Regex.Escape(name)})\.\.\.",
						Replacement, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

					// if special syntax wasn't found, leave the parameter alone, for databases that support collections directly
					if (itemCount != -1)
					{
						parameters = DbParameters.Create(nameValuePairs.Take(index).Concat(replacements).Concat(nameValuePairs.Skip(index + 1)));
						index += replacements.Count;
					}
					else
					{
						index += 1;
					}
				}
				else
				{
					index += 1;
				}
			}
		}

		return connectorCommand.Transform(commandText, parameters);
	}
}
