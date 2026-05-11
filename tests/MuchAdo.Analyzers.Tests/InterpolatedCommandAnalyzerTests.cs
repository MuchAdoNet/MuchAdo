using System.Collections.Immutable;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using NUnit.Framework;

namespace MuchAdo.Analyzers.Tests;

[TestFixture]
internal sealed class InterpolatedCommandAnalyzerTests
{
	[Test]
	public async Task CommandWithInterpolatedStringReportsDiagnostic()
	{
		var diagnostics = await GetDiagnosticsAsync("""
			using MuchAdo;

			internal sealed class Sample
			{
				public void Run(DbConnector connector, string tableName)
				{
					connector.Command($"select * from {tableName}");
				}
			}
			""");

		diagnostics.Should().ContainSingle().Which.Id.Should().Be(InterpolatedCommandAnalyzer.DiagnosticId);
	}

	[Test]
	public async Task CommandBatchWithInterpolatedStringReportsDiagnostic()
	{
		var diagnostics = await GetDiagnosticsAsync("""
			using MuchAdo;

			internal sealed class Sample
			{
				public void Run(DbConnector connector, string tableName)
				{
					connector
						.Command("select 1")
						.Command($"select * from {tableName}");
				}
			}
			""");

		diagnostics.Should().ContainSingle().Which.Id.Should().Be(InterpolatedCommandAnalyzer.DiagnosticId);
	}

	[Test]
	public async Task CommandFormatWithInterpolatedStringDoesNotReportDiagnostic()
	{
		var diagnostics = await GetDiagnosticsAsync("""
			using MuchAdo;

			internal sealed class Sample
			{
				public void Run(DbConnector connector, string tableName)
				{
					connector.CommandFormat($"select * from {Sql.Name(tableName)}");
				}
			}
			""");

		diagnostics.Should().BeEmpty();
	}

	[Test]
	public async Task CommandWithNonInterpolatedStringDoesNotReportDiagnostic()
	{
		var diagnostics = await GetDiagnosticsAsync("""
			using MuchAdo;

			internal sealed class Sample
			{
				public void Run(DbConnector connector)
				{
					connector.Command("select 1");
				}
			}
			""");

		diagnostics.Should().BeEmpty();
	}

	[Test]
	public async Task NestedInterpolationStillReportsSingleDiagnostic()
	{
		var diagnostics = await GetDiagnosticsAsync("""
			using MuchAdo;

			internal sealed class Sample
			{
				public void Run(DbConnector connector, string tableName)
				{
					connector.Command($"select * from {$"prefix_{tableName}"}");
				}
			}
			""");

		diagnostics.Should().ContainSingle().Which.Id.Should().Be(InterpolatedCommandAnalyzer.DiagnosticId);
	}

	private static async Task<ImmutableArray<Diagnostic>> GetDiagnosticsAsync(string source)
	{
		var parseOptions = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Latest);
		var compilation = CSharpCompilation.Create(
			"AnalyzerTests",
			[CSharpSyntaxTree.ParseText(source, parseOptions)],
			GetReferences(),
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

		compilation.GetDiagnostics().Where(x => x.Severity == DiagnosticSeverity.Error).Should().BeEmpty();

		var analyzers = ImmutableArray.Create<DiagnosticAnalyzer>(new InterpolatedCommandAnalyzer());
		return await compilation.WithAnalyzers(analyzers).GetAnalyzerDiagnosticsAsync();
	}

	private static IEnumerable<MetadataReference> GetReferences()
	{
		var trustedPlatformAssemblies = (string?) AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") ?? "";
		foreach (var path in trustedPlatformAssemblies.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
			yield return MetadataReference.CreateFromFile(path);

		yield return MetadataReference.CreateFromFile(typeof(DbConnector).Assembly.Location);
	}
}
