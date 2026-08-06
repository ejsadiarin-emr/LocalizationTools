using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace LocalizationAnalyzers.Analyzers;

/// <summary>
/// LOC015: Detects punctuation characters concatenated outside translatable strings.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class PunctuationOutsideStringAnalyzer : DiagnosticAnalyzer
{
    private static readonly ImmutableHashSet<string> PunctuationLiterals =
        ImmutableHashSet.Create(":", ".", ",", ";", "!", "?", ":", "。", "，", "；", "！", "？");

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
        ImmutableArray.Create(DiagnosticDescriptors.LOC015);

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
        if (HasPunctuationOutsideString(firstArg))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.LOC015,
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

        if (HasPunctuationOutsideString(assignment.Right))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.LOC015,
                assignment.Right.GetLocation(),
                assignment.Right.ToString()));
        }
    }

    private static bool HasPunctuationOutsideString(ExpressionSyntax expression)
    {
        if (expression is BinaryExpressionSyntax binary && binary.OperatorToken.IsKind(SyntaxKind.PlusToken))
        {
            if (IsPunctuationLiteral(binary.Left))
                return true;
            if (IsPunctuationLiteral(binary.Right))
                return true;
            return HasPunctuationOutsideString(binary.Left) || HasPunctuationOutsideString(binary.Right);
        }

        return false;
    }

    private static bool IsPunctuationLiteral(ExpressionSyntax expression)
    {
        if (expression is LiteralExpressionSyntax literal && literal.IsKind(SyntaxKind.StringLiteralExpression))
        {
            var text = literal.Token.ValueText;
            return PunctuationLiterals.Contains(text);
        }
        return false;
    }
}
