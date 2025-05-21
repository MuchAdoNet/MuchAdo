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
			case "list":
				m_parts.Add(FormatInfo<T>.Instance.CreateParamSourceForCollection(t));
				break;
			case "raw" when t is string { } text:
				m_parts.Add(text);
				break;
			case "tuple":
				m_parts.Add(Sql.Tuple(FormatInfo<T>.Instance.CreateParamSourceForCollection(t)));
				break;
			default:
				throw new NotSupportedException($"Format '{format}' not supported for {typeof(T).FullName}.");
		}
	}

	internal SqlSource ToSqlSource() => new FormatSqlSource(m_parts);

	private sealed class FormatInfo<T>
	{
		public static readonly FormatInfo<T> Instance = new();

		public SqlParamSource CreateParamSourceForCollection(T items) =>
			m_lazyCreateParamSourceCreator.Value(items);

		private FormatInfo()
		{
			m_lazyCreateParamSourceCreator = new(CreateParamSourceCreator);
		}

		private Func<T, SqlParamSource> CreateParamSourceCreator()
		{
			var type = typeof(T);
			var itemsParam = Expression.Parameter(type, "items");

			var itemType = type.GetInterfaces()
				.Where(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IEnumerable<>))
				.Select(x => x.GetGenericArguments()[0])
				.FirstOrDefault() ?? throw new NotSupportedException($"Type {type.FullName} does not implement IEnumerable<T>.");
			var paramsMethod = FormatInfo.GenericParamsMethod.MakeGenericMethod(itemType);
			return Expression.Lambda<Func<T, SqlParamSource>>(Expression.Call(paramsMethod, itemsParam), itemsParam).Compile();
		}

		private readonly Lazy<Func<T, SqlParamSource>> m_lazyCreateParamSourceCreator;
	}

	private static class FormatInfo
	{
		public static MethodInfo GenericParamsMethod { get; } = typeof(Sql)
			.GetMethods(BindingFlags.Public | BindingFlags.Static)
			.Single(x => x is { Name: nameof(Sql.Params), IsGenericMethod: true } &&
				x.GetGenericArguments().Length == 1 &&
				x.GetParameters().Length == 1);
	}

	private readonly List<object> m_parts;
}
