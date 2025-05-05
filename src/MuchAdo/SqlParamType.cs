using System.Data;
using System.Data.Common;

namespace MuchAdo;

/// <summary>
/// Encapsulates a parameter type.
/// </summary>
public abstract class SqlParamType
{
	/// <summary>
	/// Creates a parameter type that calls the specified delegate on the database parameter.
	/// </summary>
	public static SqlParamType Create(Action<DbParameter> action) => new ActionSqlParamType(action);

	/// <summary>
	/// Applies the parameter type to the specified database parameter.
	/// </summary>
	public abstract void Apply(IDataParameter parameter);

	private sealed class ActionSqlParamType(Action<DbParameter> action) : SqlParamType
	{
		public override void Apply(IDataParameter parameter) => action((DbParameter) parameter);
	}
}
