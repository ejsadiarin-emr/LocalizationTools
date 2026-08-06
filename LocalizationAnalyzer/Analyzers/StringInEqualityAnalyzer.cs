using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace LocalizationAnalyzers.Analyzers;

/// <summary>
/// LOC003: Detects string literals used in equality comparisons outside of conditionals.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class StringInEqualityAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(DiagnosticDescriptors.LOC003);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(AnalyzeBinaryExpression, SyntaxKind.EqualsExpression);
        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
        context.RegisterSyntaxNodeAction(AnalyzeElementAccess, SyntaxKind.ElementAccessExpression);
    }

    private void AnalyzeBinaryExpression(SyntaxNodeAnalysisContext context)
    {
        var binary = (BinaryExpressionSyntax)context.Node;

        // Skip if inside a conditional (LOC001 handles those)
        if (IsInsideConditional(context.Node))
            return;

        if (binary.Left is LiteralExpressionSyntax leftLiteral &&
            leftLiteral.IsKind(SyntaxKind.StringLiteralExpression))
        {
            ReportDiagnostic(context, leftLiteral);
        }

        if (binary.Right is LiteralExpressionSyntax rightLiteral &&
            rightLiteral.IsKind(SyntaxKind.StringLiteralExpression))
        {
            ReportDiagnostic(context, rightLiteral);
        }
    }

    private void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;

        // Skip if inside a conditional (LOC001 handles those)
        if (IsInsideConditional(context.Node))
            return;

        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
            return;

        if (memberAccess.Name.Identifier.Text != "Equals")
            return;

        foreach (var argument in invocation.ArgumentList.Arguments)
        {
            if (argument.Expression is LiteralExpressionSyntax literal &&
                literal.IsKind(SyntaxKind.StringLiteralExpression))
            {
                ReportDiagnostic(context, literal);
            }
        }
    }

    private void AnalyzeElementAccess(SyntaxNodeAnalysisContext context)
    {
        var elementAccess = (ElementAccessExpressionSyntax)context.Node;

        foreach (var argument in elementAccess.ArgumentList.Arguments)
        {
            if (argument.Expression is LiteralExpressionSyntax literal &&
                literal.IsKind(SyntaxKind.StringLiteralExpression))
            {
                ReportDiagnostic(context, literal);
            }
        }
    }

    private static bool IsInsideConditional(SyntaxNode node)
    {
        var current = node.Parent;
        while (current != null)
        {
            if (current is IfStatementSyntax ||
                current is SwitchStatementSyntax ||
                current is ConditionalExpressionSyntax)
            {
                return true;
            }
            current = current.Parent;
        }
        return false;
    }

    private void ReportDiagnostic(SyntaxNodeAnalysisContext context, LiteralExpressionSyntax literal)
    {
        var text = literal.Token.ValueText;
        var diagnostic = Diagnostic.Create(
            DiagnosticDescriptors.LOC003,
            literal.GetLocation(),
            text);
        context.ReportDiagnostic(diagnostic);
    }
}
