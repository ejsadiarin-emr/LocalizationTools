using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace LocalizationAnalyzers.Analyzers;

/// <summary>
/// LOC014: Detects English-only pluralization patterns (if/else blocks, concatenation with count).
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class EnglishOnlyPluralizationAnalyzer : DiagnosticAnalyzer
{
    private static readonly ImmutableHashSet<string> UiPropertyNames =
        ImmutableHashSet.Create(
            "Text", "Label", "Title", "Caption", "Header",
            "Content", "Placeholder", "ToolTip", "Tooltip",
            "Description", "Message", "Prompt", "Watermark");

    private static readonly ImmutableHashSet<string> OutputMethodNames =
        ImmutableHashSet.Create(
            "Write", "WriteLine",
            "Log", "LogWarning", "LogError", "LogInformation",
            "LogDebug", "LogTrace", "LogCritical", "LogNone");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(DiagnosticDescriptors.LOC014);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(AnalyzeIfStatement, SyntaxKind.IfStatement);
        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
        context.RegisterSyntaxNodeAction(AnalyzeAssignment, SyntaxKind.SimpleAssignmentExpression);
        context.RegisterSyntaxNodeAction(AnalyzeConditionalExpression, SyntaxKind.ConditionalExpression);
    }

    private void AnalyzeIfStatement(SyntaxNodeAnalysisContext context)
    {
        var ifStatement = (IfStatementSyntax)context.Node;

        if (!IsPluralCondition(ifStatement.Condition))
            return;

        var trueStrings = GetStringLiteralsInBlock(ifStatement.Statement);
        var falseStrings = ifStatement.Else != null
            ? GetStringLiteralsInBlock(ifStatement.Else.Statement)
            : new System.Collections.Generic.List<string>();

        if (trueStrings.Count > 0 && falseStrings.Count > 0)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.LOC014,
                ifStatement.Condition.GetLocation(),
                ifStatement.Condition.ToString()));
        }
    }

    private void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;
        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
            return;

        var methodName = memberAccess.Name.Identifier.Text;
        if (!OutputMethodNames.Contains(methodName))
            return;

        if (invocation.ArgumentList.Arguments.Count == 0)
            return;

        var firstArg = invocation.ArgumentList.Arguments[0].Expression;
        if (HasPluralConcatenation(firstArg))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.LOC014,
                firstArg.GetLocation(),
                firstArg.ToString()));
        }
    }

    private void AnalyzeAssignment(SyntaxNodeAnalysisContext context)
    {
        var assignment = (AssignmentExpressionSyntax)context.Node;
        if (assignment.Left is not MemberAccessExpressionSyntax memberAccess)
            return;

        var propertyName = memberAccess.Name.Identifier.Text;
        if (!UiPropertyNames.Contains(propertyName))
            return;

        if (HasPluralConcatenation(assignment.Right))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.LOC014,
                assignment.Right.GetLocation(),
                assignment.Right.ToString()));
        }
    }

    private void AnalyzeConditionalExpression(SyntaxNodeAnalysisContext context)
    {
        var conditional = (ConditionalExpressionSyntax)context.Node;

        if (!IsPluralCondition(conditional.Condition))
            return;

        var trueLiteral = conditional.WhenTrue as LiteralExpressionSyntax;
        var falseLiteral = conditional.WhenFalse as LiteralExpressionSyntax;

        bool trueHasString = trueLiteral != null && trueLiteral.IsKind(SyntaxKind.StringLiteralExpression);
        bool falseHasString = falseLiteral != null && falseLiteral.IsKind(SyntaxKind.StringLiteralExpression);

        if (trueHasString && falseHasString)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.LOC014,
                conditional.Condition.GetLocation(),
                conditional.Condition.ToString()));
        }
    }

    private static bool IsPluralCondition(ExpressionSyntax condition)
    {
        if (condition is BinaryExpressionSyntax binary)
        {
            var isCompareToOne =
                IsNumericLiteral(binary.Right, 1) || IsNumericLiteral(binary.Left, 1);

            if (isCompareToOne && (IsCountOrSize(binary.Left) || IsCountOrSize(binary.Right)))
                return true;
        }

        return false;
    }

    private static bool HasPluralConcatenation(ExpressionSyntax expression)
    {
        if (expression is BinaryExpressionSyntax binary && binary.OperatorToken.IsKind(SyntaxKind.PlusToken))
        {
            if (binary.Left is LiteralExpressionSyntax leftLiteral && leftLiteral.IsKind(SyntaxKind.StringLiteralExpression))
            {
                if (IsCountOrSize(binary.Right))
                    return true;
            }
            if (binary.Right is LiteralExpressionSyntax rightLiteral && rightLiteral.IsKind(SyntaxKind.StringLiteralExpression))
            {
                if (IsCountOrSize(binary.Left))
                    return true;
            }
            return HasPluralConcatenation(binary.Left) || HasPluralConcatenation(binary.Right);
        }

        return false;
    }

    private static System.Collections.Generic.List<string> GetStringLiteralsInBlock(StatementSyntax statement)
    {
        var result = new System.Collections.Generic.List<string>();
        if (statement is BlockSyntax block)
        {
            foreach (var stmt in block.Statements)
            {
                if (stmt is ExpressionStatementSyntax exprStmt && exprStmt.Expression is AssignmentExpressionSyntax assignment)
                {
                    if (assignment.Right is LiteralExpressionSyntax literal && literal.IsKind(SyntaxKind.StringLiteralExpression))
                    {
                        result.Add(literal.Token.ValueText);
                    }
                }
            }
        }
        return result;
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
