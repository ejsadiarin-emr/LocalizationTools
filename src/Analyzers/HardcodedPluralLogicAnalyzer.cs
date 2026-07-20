using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace LocalizationAnalyzers.Analyzers;

/// <summary>
/// LOC007: Detects hardcoded pluralization logic (ternary comparing to 1/0 with string literals).
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class HardcodedPluralLogicAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(DiagnosticDescriptors.LOC007);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(AnalyzeConditionalExpression, SyntaxKind.ConditionalExpression);
    }

    private void AnalyzeConditionalExpression(SyntaxNodeAnalysisContext context)
    {
        var conditional = (ConditionalExpressionSyntax)context.Node;

        if (!IsPluralComparison(conditional.Condition))
            return;

        var whenTrue = conditional.WhenTrue;
        var whenFalse = conditional.WhenFalse;

        var hasStringLiteral = (whenTrue is LiteralExpressionSyntax l1 && l1.IsKind(SyntaxKind.StringLiteralExpression)) ||
                               (whenFalse is LiteralExpressionSyntax l2 && l2.IsKind(SyntaxKind.StringLiteralExpression));

        if (hasStringLiteral)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.LOC007,
                conditional.GetLocation(),
                conditional.Condition.ToString()));
        }
    }

    private static bool IsPluralComparison(ExpressionSyntax condition)
    {
        if (condition is BinaryExpressionSyntax binary)
        {
            var isCompareToZeroOrOne =
                IsNumericLiteral(binary.Right, 0) || IsNumericLiteral(binary.Right, 1) ||
                IsNumericLiteral(binary.Left, 0) || IsNumericLiteral(binary.Left, 1);

            if (isCompareToZeroOrOne && IsCountOrSize(binary.Left) || IsCountOrSize(binary.Right))
                return true;
        }

        if (condition is InvocationExpressionSyntax invocation &&
            invocation.Expression is MemberAccessExpressionSyntax memberAccess)
        {
            var methodName = memberAccess.Name.Identifier.Text;
            return methodName == "Equals" && invocation.ArgumentList.Arguments.Count == 1;
        }

        return false;
    }

    private static bool IsNumericLiteral(ExpressionSyntax expr, int value)
    {
        if (expr is LiteralExpressionSyntax literal && literal.IsKind(SyntaxKind.NumericLiteralExpression))
        {
            return literal.Token.ValueText == value.ToString();
        }
        return false;
    }

    private static bool IsCountOrSize(ExpressionSyntax expr)
    {
        if (expr is MemberAccessExpressionSyntax memberAccess)
        {
            var name = memberAccess.Name.Identifier.Text;
            return name == "Count" || name == "Length" || name == "Size";
        }

        if (expr is IdentifierNameSyntax identifier)
        {
            var name = identifier.Identifier.Text;
            return name.ToLowerInvariant().Contains("count") ||
                   name.ToLowerInvariant().Contains("num") ||
                   name.ToLowerInvariant().Contains("total");
        }

        return false;
    }
}
