using System.Collections.ObjectModel;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using MuchAdo.Sources;

namespace MuchAdo;

/// <summary>
/// Encapsulates parameters.
/// </summary>
public abstract class SqlParamSource : SqlSource
{
	/// <summary>
	/// Filters the parameters by name.
	/// </summary>
	public SqlParamSource Where(Func<string, bool> nameMatches) =>
		new FilteredSqlParamSource(this, nameMatches ?? throw new ArgumentNullException(nameof(nameMatches)));

	/// <summary>
	/// Transforms the parameter names using the specified function.
	/// </summary>
	public SqlParamSource Renamed(Func<string, string> transform) =>
		new RenamedSqlParamSource(this, transform ?? throw new ArgumentNullException(nameof(transform)));

	/// <summary>
	/// Enumerates the parameters in this source.
	/// </summary>
	public IEnumerable<SqlParam<object?>> Enumerate()
	{
		var target = new EnumerateParameterTarget();
		SubmitParameters(target);
		return target.Items;
	}

	/// <summary>
	/// Creates a parameter source from a <see cref="DbParameter" />.
	/// </summary>
	[SuppressMessage("Usage", "CA2225:Operator overloads have named alternates", Justification = "Use Sql.Param.")]
	public static implicit operator SqlParamSource(DbParameter parameter) => Sql.Param(parameter);

	internal abstract void SubmitParameters(ISqlParamTarget target);

	internal override void Render(DbConnectorCommandBuilder builder)
	{
		var target = new ParamTarget(builder);
		SubmitParameters(target);
	}

	private readonly struct ParamTarget(DbConnectorCommandBuilder builder) : ISqlParamTarget
	{
		public void AcceptParameter<T>(string name, T value, SqlParamType? type)
		{
			if (m_originalTextLength != builder.TextLength)
				builder.AppendText(", ");

			if (string.IsNullOrEmpty(name))
			{
				builder.AppendParameterValue(value, type);
			}
			else
			{
				builder.AppendText(builder.Syntax.NamedParameterPrefix);
				builder.AppendText(name);
				builder.SubmitParameters(Sql.NamedParam(name, value, type));
			}
		}

		private readonly int m_originalTextLength = builder.TextLength;
	}

	private sealed class EnumerateParameterTarget : ISqlParamTarget
	{
		public Collection<SqlParam<object?>> Items { get; } = new();

		public void AcceptParameter<T>(string name, T value, SqlParamType? type)
		{
			if (string.IsNullOrEmpty(name))
				Items.Add(type is null ? Sql.Param<object?>(value) : Sql.Param<object?>(value, type));
			else
				Items.Add(type is null ? Sql.NamedParam<object?>(name, value) : Sql.NamedParam<object?>(name, value, type));
		}
	}
}
