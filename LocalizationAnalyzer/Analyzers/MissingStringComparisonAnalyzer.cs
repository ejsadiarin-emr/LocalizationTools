using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace LocalizationAnalyzers.Analyzers;

/// <summary>
/// LOC006: Detects string method calls without StringComparison parameter.
/// Uses semantic analysis to verify the method is on a string type and check parameter types.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class MissingStringComparisonAnalyzer : DiagnosticAnalyzer
{
    private static readonly ImmutableHashSet<string> StringComparisonMethods =
        ImmutableHashSet.Create(
            "Contains", "StartsWith", "EndsWith", "IndexOf",
            "Replace", "Equals", "Compare");

    private static readonly ImmutableHashSet<string> CultureMethods =
        ImmutableHashSet.Create("ToLower", "ToUpper");

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

        // Get the symbol to verify this is actually a string method
        var symbol = context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol as IMethodSymbol;
        if (symbol == null)
            return;

        // Verify the receiver type is string (or the method is a static string method)
        var receiverType = symbol.ReceiverType;
        if (receiverType == null)
            return;

        var typeName = receiverType.OriginalDefinition.ToDisplayString();
        if (typeName != "string")
            return;

        if (StringComparisonMethods.Contains(methodName))
        {
            // Semantic check: does any argument have StringComparison type?
            var hasStringComparison = symbol.Parameters.Any(p =>
                p.Type.OriginalDefinition.ToDisplayString() == "System.StringComparison");

            if (!hasStringComparison)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.LOC006,
                    invocation.GetLocation(),
                    methodName));
            }
        }
        else if (CultureMethods.Contains(methodName))
        {
            // Semantic check: does the method have zero arguments?
            if (symbol.Parameters.Length == 0)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.LOC006,
                    invocation.GetLocation(),
                    methodName));
            }
        }
    }
}
