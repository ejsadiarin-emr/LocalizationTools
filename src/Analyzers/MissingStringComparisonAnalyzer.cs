using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace LocalizationAnalyzers.Analyzers;

/// <summary>
/// LOC006: Detects string method calls without StringComparison parameter.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class MissingStringComparisonAnalyzer : DiagnosticAnalyzer
{
    private static readonly ImmutableHashSet<string> StringComparisonMethods =
        ImmutableHashSet.Create(
            "Contains", "StartsWith", "EndsWith", "IndexOf",
            "Replace", "Equals", "Compare", "TrimStart", "TrimEnd");

    private static readonly ImmutableHashSet<string> CultureMethods =
        ImmutableHashSet.Create("ToLower", "ToUpper", "ToLowerInvariant", "ToUpperInvariant");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(DiagnosticDescriptors.LOC006);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
    }

    private void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;
        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
            return;

        var methodName = memberAccess.Name.Identifier.Text;

        if (StringComparisonMethods.Contains(methodName))
        {
            if (invocation.ArgumentList.Arguments.Count == 1 &&
                invocation.ArgumentList.Arguments[0].Expression is LiteralExpressionSyntax)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.LOC006,
                    invocation.GetLocation(),
                    methodName));
            }
        }
        else if (CultureMethods.Contains(methodName))
        {
            if (invocation.ArgumentList.Arguments.Count == 0)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.LOC006,
                    invocation.GetLocation(),
                    methodName));
            }
        }
    }
}
