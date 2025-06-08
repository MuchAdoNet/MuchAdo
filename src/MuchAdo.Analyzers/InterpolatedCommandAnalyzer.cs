using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace MuchAdo.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class InterpolatedCommandAnalyzer : DiagnosticAnalyzer
{
	public override void Initialize(AnalysisContext context)
	{
		context.EnableConcurrentExecution();
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

		context.RegisterCompilationStartAction(compilationStartAnalysisContext =>
		{
			var dbConnectorType = compilationStartAnalysisContext.Compilation.GetTypeByMetadataName("MuchAdo.DbConnector");
			var dbConnectorCommandBatchType = compilationStartAnalysisContext.Compilation.GetTypeByMetadataName("MuchAdo.DbConnectorCommandBatch");
			if (dbConnectorType is not null || dbConnectorCommandBatchType is not null)
			{
				compilationStartAnalysisContext.RegisterSyntaxNodeAction(syntaxNodeAnalysisContext =>
				{
					if (syntaxNodeAnalysisContext.Node is InvocationExpressionSyntax { ArgumentList.Arguments: var invocationArguments } invocation &&
						invocationArguments.Count != 0 &&
						invocationArguments[0].Expression is InterpolatedStringExpressionSyntax &&
						syntaxNodeAnalysisContext.SemanticModel.GetSymbolInfo(invocation.Expression)
							.Symbol is IMethodSymbol { Name: "Command", Parameters: var methodParameters, ContainingType: var methodContainingType } &&
						methodParameters.Length != 0 &&
						methodParameters[0].Type.SpecialType == SpecialType.System_String &&
						(SymbolEqualityComparer.Default.Equals(methodContainingType, dbConnectorType) ||
							SymbolEqualityComparer.Default.Equals(methodContainingType, dbConnectorCommandBatchType)))
					{
						syntaxNodeAnalysisContext.ReportDiagnostic(Diagnostic.Create(s_rule, invocation.GetLocation()));
					}
				}, SyntaxKind.InvocationExpression);
			}
		});
	}

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_rule);

	public const string DiagnosticId = "MUCH0001";

	private static readonly DiagnosticDescriptor s_rule = new(
		id: DiagnosticId,
		title: "Use DbConnector.CommandFormat",
		messageFormat: "Command should not be used with an interpolated string. Use CommandFormat instead.",
		category: "Usage",
		defaultSeverity: DiagnosticSeverity.Warning,
		isEnabledByDefault: true,
		helpLinkUri: $"https://muchado.net/analyzers#{DiagnosticId}");
}
