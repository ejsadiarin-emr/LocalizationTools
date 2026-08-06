using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace LocalizationAnalyzers.Analyzers;

/// <summary>
/// LOC004: Detects string concatenation in output contexts (Console, Debug, logging, UI).
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class StringConcatenationAnalyzer : DiagnosticAnalyzer
{
    private static readonly ImmutableHashSet<string> OutputMethodNames =
        ImmutableHashSet.Create(
            "Write", "WriteLine",
            "Log", "LogWarning", "LogError", "LogInformation",
            "LogDebug", "LogTrace", "LogCritical", "LogNone");

    private static readonly ImmutableHashSet<string> OutputClassNames =
        ImmutableHashSet.Create(
            "Console", "Debug", "Trace", "Logger", "ILogger");

    private static readonly ImmutableHashSet<string> UiPropertyNames =
        ImmutableHashSet.Create(
            "Text", "Label", "Title", "Caption", "Header",
            "Content", "Placeholder", "ToolTip", "Tooltip",
            "Description", "Message", "Prompt", "Watermark");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(DiagnosticDescriptors.LOC004);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
        context.RegisterSyntaxNodeAction(AnalyzeAssignment, SyntaxKind.SimpleAssignmentExpression);
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
        if (HasStringConcatenation(firstArg))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.LOC004,
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

        if (HasStringConcatenation(assignment.Right))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.LOC004,
                assignment.Right.GetLocation(),
                assignment.Right.ToString()));
        }
    }

    private static bool HasStringConcatenation(ExpressionSyntax expression)
    {
        if (expression is BinaryExpressionSyntax binary && binary.OperatorToken.IsKind(SyntaxKind.PlusToken))
        {
            if (binary.Left is LiteralExpressionSyntax leftLiteral && leftLiteral.IsKind(SyntaxKind.StringLiteralExpression))
                return true;
            if (binary.Right is LiteralExpressionSyntax rightLiteral && rightLiteral.IsKind(SyntaxKind.StringLiteralExpression))
                return true;
            if (HasStringConcatenation(binary.Left) || HasStringConcatenation(binary.Right))
                return true;
        }

        if (expression is InterpolatedStringExpressionSyntax)
            return true;

        return false;
    }
}
