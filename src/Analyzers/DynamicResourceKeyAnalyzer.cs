using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace LocalizationAnalyzers.Analyzers;

/// <summary>
/// LOC013: Detects dynamic/computed resource keys in localizer indexers.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class DynamicResourceKeyAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(DiagnosticDescriptors.LOC013);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(AnalyzeElementAccess, SyntaxKind.ElementAccessExpression);
        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
    }

    private void AnalyzeElementAccess(SyntaxNodeAnalysisContext context)
    {
        var elementAccess = (ElementAccessExpressionSyntax)context.Node;

        if (!IsLocalizerAccess(elementAccess.Expression))
            return;

        if (elementAccess.ArgumentList.Arguments.Count != 1)
            return;

        var argument = elementAccess.ArgumentList.Arguments[0].Expression;
        if (IsDynamicKey(argument))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.LOC013,
                argument.GetLocation(),
                argument.ToString()));
        }
    }

    private void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;
        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
            return;

        var methodName = memberAccess.Name.Identifier.Text;
        if (methodName != "Item" && methodName != "get_Item")
            return;

        if (!IsLocalizerAccess(memberAccess.Expression))
            return;

        if (invocation.ArgumentList.Arguments.Count != 1)
            return;

        var argument = invocation.ArgumentList.Arguments[0].Expression;
        if (IsDynamicKey(argument))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.LOC013,
                argument.GetLocation(),
                argument.ToString()));
        }
    }

    private static bool IsLocalizerAccess(ExpressionSyntax expression)
    {
        if (expression is IdentifierNameSyntax identifier)
        {
            var name = identifier.Identifier.Text;
            return name.Contains("localizer", System.StringComparison.OrdinalIgnoreCase)
                || name.Contains("Localize", System.StringComparison.Ordinal);
        }

        return false;
    }

    private static bool IsDynamicKey(ExpressionSyntax expression)
    {
        if (expression is InterpolatedStringExpressionSyntax)
            return true;

        if (expression is BinaryExpressionSyntax binary && binary.OperatorToken.IsKind(SyntaxKind.PlusToken))
            return true;

        if (expression is IdentifierNameSyntax)
            return true;

        if (expression is ConditionalExpressionSyntax)
            return true;

        return false;
    }
}
