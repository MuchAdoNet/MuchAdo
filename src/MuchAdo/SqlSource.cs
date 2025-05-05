using System.Diagnostics.CodeAnalysis;
using MuchAdo.Sources;

namespace MuchAdo;

/// <summary>
/// Encapsulates parameterized SQL.
/// </summary>
public abstract class SqlSource
{
	/// <summary>
	/// Concatenates two SQL fragments.
	/// </summary>
	[SuppressMessage("Usage", "CA2225:Operator overloads have named alternates", Justification = "Use Sql.Concat.")]
	public static SqlSource operator +(SqlSource a, SqlSource b) =>
		new AddSqlSource(a ?? throw new ArgumentNullException(nameof(a)), b ?? throw new ArgumentNullException(nameof(b)));

	/// <summary>
	/// Renders the SQL source using ANSI syntax.
	/// </summary>
	public override string ToString() => ToString(SqlSyntax.Ansi);

	/// <summary>
	/// Renders the SQL source using the specified syntax.
	/// </summary>
	public string ToString(SqlSyntax syntax)
	{
		var commandBuilder = new DbConnectorCommandBuilder(syntax, buildText: true, paramTarget: null);
		Render(commandBuilder);
		return commandBuilder.GetText();
	}

	internal abstract void Render(DbConnectorCommandBuilder builder);
}
