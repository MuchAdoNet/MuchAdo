using System.Linq.Expressions;
using System.Reflection;

namespace MuchAdo;

internal sealed class DbDtoProperty<T>
{
	public DbDtoProperty(PropertyInfo propertyInfo, string? columnName)
	{
		MemberInfo = propertyInfo;
		Name = propertyInfo.Name;
		ValueType = propertyInfo.PropertyType;
		IsReadOnly = propertyInfo.SetMethod?.IsPublic is not true;
		ColumnName = columnName;
		m_lazySubmitParameter = new(CreateSubmitParameter);
	}

	public DbDtoProperty(FieldInfo fieldInfo, string? columnName)
	{
		MemberInfo = fieldInfo;
		Name = fieldInfo.Name;
		ValueType = fieldInfo.FieldType;
		IsReadOnly = fieldInfo.IsInitOnly;
		ColumnName = columnName;
		m_lazySubmitParameter = new(CreateSubmitParameter);
	}

	public MemberInfo MemberInfo { get; }

	public string Name { get; }

	public Type ValueType { get; }

	public bool IsReadOnly { get; }

	public string? ColumnName { get; }

	public void SubmitParameter(ISqlParamTarget target, string name, T source, SqlParamType? type) => m_lazySubmitParameter.Value(target, name, source, type);

	private Action<ISqlParamTarget, string, T, SqlParamType?> CreateSubmitParameter()
	{
		var targetParam = Expression.Parameter(typeof(ISqlParamTarget), "target");
		var nameParam = Expression.Parameter(typeof(string), "name");
		var sourceParam = Expression.Parameter(typeof(T), "source");
		var typeParam = Expression.Parameter(typeof(SqlParamType), "type");

		var getValue = MemberInfo is PropertyInfo propertyInfo
			? Expression.Property(sourceParam, propertyInfo)
			: Expression.Field(sourceParam, (FieldInfo) MemberInfo);

		var acceptMethod = typeof(ISqlParamTarget)
			.GetMethods(BindingFlags.Public | BindingFlags.Instance)
			.Single(x => x is { Name: "AcceptParameter", IsGenericMethod: true } &&
				x.GetGenericArguments().Length == 1 &&
				x.GetParameters() is [var p0, var p1, var p2] &&
				p0.ParameterType == typeof(string) &&
				p1.ParameterType.IsGenericParameter &&
				p2.ParameterType == typeof(SqlParamType)).MakeGenericMethod(ValueType);

		return Expression.Lambda<Action<ISqlParamTarget, string, T, SqlParamType?>>(
			Expression.Call(targetParam, acceptMethod, nameParam, getValue, typeParam), targetParam, nameParam, sourceParam, typeParam).Compile();
	}

	private readonly Lazy<Action<ISqlParamTarget, string, T, SqlParamType?>> m_lazySubmitParameter;
}
