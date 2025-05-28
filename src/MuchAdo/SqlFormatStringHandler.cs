using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using MuchAdo.Sources;

namespace MuchAdo;

/// <summary>
/// Used by <see cref="Sql.Format" />.
/// </summary>
[InterpolatedStringHandler]
public readonly ref struct SqlFormatStringHandler
{
	public SqlFormatStringHandler(int literalLength, int formattedCount)
	{
		m_parts = new(capacity: formattedCount * 2 + 1);
	}

	public void AppendLiteral(string s) => m_parts.Add(s);

	public void AppendFormatted<T>(T t) => m_parts.Add(t as SqlSource ?? new FormatParamSqlSource<T>(t));

	public void AppendFormatted<T>(T t, string? format)
	{
		switch (format)
		{
			case null or "":
				AppendFormatted(t);
				break;
			case "set":
				m_parts.Add(FormatInfo<T>.Instance.CreateParamSet(t));
				break;
			default:
				throw new NotSupportedException($"Format '{format}' not supported for {typeof(T).FullName}.");
		}
	}

	internal SqlSource ToSqlSource() => new FormatSqlSource(m_parts);

	private sealed class FormatInfo<T>
	{
		public static readonly FormatInfo<T> Instance = new();

		public SqlSource CreateParamSet(T items) => m_lazyCreateParamSetCreator.Value(items);

		private FormatInfo()
		{
			m_lazyCreateParamSetCreator = new(CreateParamSetCreator);
		}

		private Func<T, SqlSource> CreateParamSetCreator()
		{
			var type = typeof(T);
			var itemsParam = Expression.Parameter(type, "items");

			var itemType = type.GetInterfaces().Prepend(type)
				.Where(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IEnumerable<>))
				.Select(x => x.GetGenericArguments()[0])
				.FirstOrDefault() ?? throw new NotSupportedException($"Type {type.FullName} must implement IEnumerable<T>.");

			if (typeof(SqlSource).IsAssignableFrom(itemType))
				throw new NotSupportedException("Format specifier not supported for collections of SqlSource.");

			var paramsMethod = FormatInfo.GenericParamSetMethod.MakeGenericMethod(itemType);
			return Expression.Lambda<Func<T, SqlSource>>(Expression.Call(paramsMethod, itemsParam), itemsParam).Compile();
		}

		private readonly Lazy<Func<T, SqlSource>> m_lazyCreateParamSetCreator;
	}

	private static class FormatInfo
	{
		public static MethodInfo GenericParamSetMethod { get; } = typeof(Sql)
			.GetMethods(BindingFlags.Public | BindingFlags.Static)
			.Single(x => x is { Name: nameof(Sql.ParamSet), IsGenericMethod: true } &&
				x.GetGenericArguments().Length == 1 &&
				x.GetParameters().Length == 1);
	}

	private readonly List<object> m_parts;
}
