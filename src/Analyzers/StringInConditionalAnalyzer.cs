using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace LocalizationAnalyzers.Analyzers;

/// <summary>
/// LOC001: Detects string literals used in conditional expressions.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class StringInConditionalAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(DiagnosticDescriptors.LOC001);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(AnalyzeIfStatement, SyntaxKind.IfStatement);
        context.RegisterSyntaxNodeAction(AnalyzeSwitchStatement, SyntaxKind.SwitchStatement);
        context.RegisterSyntaxNodeAction(AnalyzeConditionalExpression, SyntaxKind.ConditionalExpression);
    }

    private void AnalyzeIfStatement(SyntaxNodeAnalysisContext context)
    {
        var ifStatement = (IfStatementSyntax)context.Node;
        AnalyzeConditionalExpressionSyntax(context, ifStatement.Condition);
    }

    private void AnalyzeSwitchStatement(SyntaxNodeAnalysisContext context)
    {
        var switchStatement = (SwitchStatementSyntax)context.Node;

        foreach (var section in switchStatement.Sections)
        {
            foreach (var label in section.Labels)
            {
                if (label is CaseSwitchLabelSyntax caseLabel &&
                    caseLabel.Value is LiteralExpressionSyntax literal &&
                    literal.IsKind(SyntaxKind.StringLiteralExpression))
                {
                    ReportDiagnostic(context, literal);
                }
            }
        }
    }

    private void AnalyzeConditionalExpression(SyntaxNodeAnalysisContext context)
    {
        var conditional = (ConditionalExpressionSyntax)context.Node;
        AnalyzeConditionalExpressionSyntax(context, conditional.Condition);
        AnalyzeExpressionForStringLiterals(context, conditional.WhenTrue);
        AnalyzeExpressionForStringLiterals(context, conditional.WhenFalse);
    }

    private void AnalyzeConditionalExpressionSyntax(SyntaxNodeAnalysisContext context, ExpressionSyntax condition)
    {
        // Check for == comparisons with string literals
        if (condition is BinaryExpressionSyntax binary &&
            binary.IsKind(SyntaxKind.EqualsExpression))
        {
            CheckBinaryExpressionForStringLiterals(context, binary);
        }
        // Check for .Equals() calls with string literals
        else if (condition is InvocationExpressionSyntax invocation &&
                 invocation.Expression is MemberAccessExpressionSyntax memberAccess &&
                 memberAccess.Name.Identifier.Text == "Equals")
        {
            CheckInvocationForStringLiterals(context, invocation);
        }
        // Check for string literal directly used as condition
        else if (condition is LiteralExpressionSyntax literal &&
                 literal.IsKind(SyntaxKind.StringLiteralExpression))
        {
            ReportDiagnostic(context, literal);
        }
    }

    private void CheckBinaryExpressionForStringLiterals(SyntaxNodeAnalysisContext context, BinaryExpressionSyntax binary)
    {
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

    private void CheckInvocationForStringLiterals(SyntaxNodeAnalysisContext context, InvocationExpressionSyntax invocation)
    {
        foreach (var argument in invocation.ArgumentList.Arguments)
        {
            if (argument.Expression is LiteralExpressionSyntax literal &&
                literal.IsKind(SyntaxKind.StringLiteralExpression))
            {
                ReportDiagnostic(context, literal);
            }
        }
    }

    private void AnalyzeExpressionForStringLiterals(SyntaxNodeAnalysisContext context, ExpressionSyntax expression)
    {
        if (expression is LiteralExpressionSyntax literal &&
            literal.IsKind(SyntaxKind.StringLiteralExpression))
        {
            ReportDiagnostic(context, literal);
        }
    }

    private void ReportDiagnostic(SyntaxNodeAnalysisContext context, LiteralExpressionSyntax literal)
    {
        var text = literal.Token.ValueText;
        var diagnostic = Diagnostic.Create(
            DiagnosticDescriptors.LOC001,
            literal.GetLocation(),
            text);
        context.ReportDiagnostic(diagnostic);
    }
}
