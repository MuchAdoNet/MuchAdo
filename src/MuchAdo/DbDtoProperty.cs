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
		m_lazyCreateParameter = new(CreateParameterCreator);
	}

	public DbDtoProperty(FieldInfo fieldInfo, string? columnName)
	{
		MemberInfo = fieldInfo;
		Name = fieldInfo.Name;
		ValueType = fieldInfo.FieldType;
		IsReadOnly = fieldInfo.IsInitOnly;
		ColumnName = columnName;
		m_lazyCreateParameter = new(CreateParameterCreator);
	}

	public MemberInfo MemberInfo { get; }

	public string Name { get; }

	public Type ValueType { get; }

	public bool IsReadOnly { get; }

	public string? ColumnName { get; }

	public DbParameters CreateParameter(string name, T valueSource) => m_lazyCreateParameter.Value(name, valueSource);

	private Func<string, T, DbParameters> CreateParameterCreator()
	{
		var nameParam = Expression.Parameter(typeof(string), "name");
		var sourceParam = Expression.Parameter(typeof(T), "source");

		var getValue = MemberInfo is PropertyInfo propertyInfo
			? Expression.Property(sourceParam, propertyInfo)
			: Expression.Field(sourceParam, (FieldInfo) MemberInfo);

		var createMethod = typeof(DbParameters)
			.GetMethods(BindingFlags.Public | BindingFlags.Static)
			.Single(x => x is { Name: "Create", IsGenericMethod: true } &&
				x.GetGenericArguments().Length == 1 &&
				x.GetParameters() is [var p0, var p1] &&
				p0.ParameterType == typeof(string) &&
				p1.ParameterType.IsGenericParameter).MakeGenericMethod(ValueType);

		return Expression.Lambda<Func<string, T, DbParameters>>(
			Expression.Call(createMethod, nameParam, getValue), nameParam, sourceParam).Compile();
	}

	private readonly Lazy<Func<string, T, DbParameters>> m_lazyCreateParameter;
}
