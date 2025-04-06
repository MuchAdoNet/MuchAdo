using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace MuchAdo;

internal static class DbDtoInfo
{
	public static DbDtoInfo<T> GetInfo<T>() => DbDtoInfo<T>.Instance;
}

[SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "Same name.")]
internal sealed class DbDtoInfo<T>
{
	internal static readonly DbDtoInfo<T> Instance = new();

	private DbDtoInfo()
	{
		var properties = new List<DbDtoProperty<T>>();

		var type = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
		foreach (var memberInfo in type.GetRuntimeProperties().Where(IsPublicNonStaticProperty).AsEnumerable<MemberInfo>().Concat(type.GetRuntimeFields().Where(IsPublicNonStaticField)))
		{
			// use Name of ColumnAttribute if specified (any namespace)
			var columnName = memberInfo
				.GetCustomAttributes()
				.Where(x => x.GetType().Name == "ColumnAttribute")
				.Select(x => x.GetType().GetRuntimeProperties().FirstOrDefault(p => string.Equals(p.Name, "Name", StringComparison.OrdinalIgnoreCase))?.GetValue(x) as string)
				.FirstOrDefault(x => x is not null);

			properties.Add(memberInfo is PropertyInfo propertyInfo
				? new DbDtoProperty<T>(propertyInfo, columnName)
				: new DbDtoProperty<T>((FieldInfo) memberInfo, columnName));
		}

		properties.TrimExcess();
		Properties = properties;

		m_lazyCreators = new Lazy<Creator?[]>(
			() => GetCreators().OrderBy(x => x?.Parameters.Length ?? 0).ToArray());

		static bool IsPublicNonStaticProperty(PropertyInfo info) => info.GetMethod is { IsPublic: true, IsStatic: false };

		static bool IsPublicNonStaticField(FieldInfo info) => info is { IsPublic: true, IsStatic: false };
	}

	public IReadOnlyList<DbDtoProperty<T>> Properties { get; }

	public IReadOnlyList<Creator?> Creators => m_lazyCreators.Value;

	private IEnumerable<Creator?> GetCreators()
	{
		var type = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);

		// value types have a default constructor
		if (type.IsValueType)
			yield return null;

		foreach (var constructor in type.GetConstructors())
		{
			var parameters = constructor.GetParameters();
			if (parameters.Length == 0 && !type.IsValueType)
			{
				// null means default constructor
				yield return null;
			}
			else
			{
				var isCreator = true;
				var properties = new DbDtoProperty<T>[parameters.Length];
				object?[]? defaultValues = null;
				for (var index = 0; index < parameters.Length; index++)
				{
					var parameter = parameters[index];
					var property = parameter.Name is { } name ? Properties.FirstOrDefault(x => string.Equals(name, x.Name, StringComparison.OrdinalIgnoreCase)) : null;
					if (property is null)
					{
						isCreator = false;
						break;
					}
					properties[index] = property;
					if (parameter.HasDefaultValue)
						(defaultValues ??= new object?[parameters.Length])[index] = parameter.DefaultValue;
				}

				if (isCreator)
					yield return new Creator(constructor, properties, defaultValues);
			}
		}
	}

	public sealed class Creator
	{
		public Creator(ConstructorInfo constructor, DbDtoProperty<T>[] parameters, object?[]? defaultValues)
		{
			Constructor = constructor;
			Parameters = parameters;
			DefaultValues = defaultValues;

			m_propertyIndices = new Dictionary<DbDtoProperty<T>, int>();
			for (var index = 0; index < parameters.Length; index++)
				m_propertyIndices.Add(parameters[index], index);
		}

		public ConstructorInfo Constructor { get; }

		public DbDtoProperty<T>[] Parameters { get; }

		public object?[]? DefaultValues { get; }

		public int? GetPropertyParameterIndex(DbDtoProperty<T> property) =>
			m_propertyIndices.TryGetValue(property, out var index) ? index : null;

		private readonly Dictionary<DbDtoProperty<T>, int> m_propertyIndices;
	}

	private readonly Lazy<Creator?[]> m_lazyCreators;
}
