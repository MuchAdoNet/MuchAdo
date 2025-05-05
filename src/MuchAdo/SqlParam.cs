namespace MuchAdo;

/// <summary>
/// A parameter with a value of the specified type. Use <see cref="Sql" /> to create.
/// </summary>
public class SqlParam<T> : SqlParamSource
{
	/// <summary>
	/// The value of the parameter.
	/// </summary>
	public T Value { get; set; }

	/// <summary>
	/// The name of the parameter, or empty if it is unnamed.
	/// </summary>
	public virtual string Name => "";

	/// <summary>
	/// The type of the parameter, or null if it is not specified.
	/// </summary>
	public virtual SqlParamType? Type => null;

	internal SqlParam(T value) => Value = value;

	internal virtual bool IsReused => false;

	internal override void SubmitParameters(ISqlParamTarget target) => target.AcceptParameter(Name, Value, Type);

	internal override void Render(DbConnectorCommandBuilder builder)
	{
		if (string.IsNullOrEmpty(Name))
		{
			builder.AppendParameterValue(Value, Type, identity: IsReused ? this : null);
		}
		else
		{
			builder.AppendText(builder.Syntax.NamedParameterPrefix);
			builder.AppendText(Name);
			builder.SubmitParameters(this);
		}
	}
}
