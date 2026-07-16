using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace LocalizationAnalyzers.Analyzers;

/// <summary>
/// LOC010: Detects display strings not routed through Localize().
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class DisplayStringAnalyzer : DiagnosticAnalyzer
{
    private static readonly ImmutableHashSet<string> UiPropertyNames =
        ImmutableHashSet.Create(
            "Text", "Label", "Title", "Caption", "Header",
            "Content", "Placeholder", "ToolTip", "Tooltip",
            "Description", "Message", "Prompt", "Watermark");

    private static readonly ImmutableHashSet<string> UiTypeSuffixes =
        ImmutableHashSet.Create(
            "Button", "Label", "TextBox", "Dialog", "MessageBox",
            "Window", "MenuItem", "CheckBox", "RadioButton", "ComboBox",
            "ListBox", "TabControl", "Panel", "Frame", "Page");

    private static readonly ImmutableHashSet<string> LoggingMethodNames =
        ImmutableHashSet.Create(
            "WriteLine", "Write", "Log", "LogWarning", "LogError", "LogInformation",
            "LogDebug", "LogTrace", "LogCritical", "LogNone");

    private static readonly ImmutableHashSet<string> DebugMethodNames =
        ImmutableHashSet.Create(
            "WriteLine", "Write", "Print");

    private static readonly ImmutableHashSet<string> DebugClassNames =
        ImmutableHashSet.Create(
            "Debug", "Trace");

    private static readonly ImmutableHashSet<string> ExcludePropertyNames =
        ImmutableHashSet.Create(
            "Name", "Tag", "Key", "Id", "Type", "Path");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(DiagnosticDescriptors.LOC010);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(AnalyzeAssignment, SyntaxKind.SimpleAssignmentExpression);
        context.RegisterSyntaxNodeAction(AnalyzeObjectCreation, SyntaxKind.ObjectCreationExpression);
        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
    }

    private void AnalyzeAssignment(SyntaxNodeAnalysisContext context)
    {
        var assignment = (AssignmentExpressionSyntax)context.Node;

        string? propertyName = null;

        if (assignment.Left is IdentifierNameSyntax identifierName)
        {
            propertyName = identifierName.Identifier.Text;
        }
        else if (assignment.Left is MemberAccessExpressionSyntax memberAccess)
        {
            propertyName = memberAccess.Name.Identifier.Text;
        }

        if (propertyName is null)
            return;

        if (!UiPropertyNames.Contains(propertyName))
            return;

        if (ExcludePropertyNames.Contains(propertyName))
            return;

        AnalyzeExpression(context, assignment.Right);
    }

    private void AnalyzeObjectCreation(SyntaxNodeAnalysisContext context)
    {
        var creation = (ObjectCreationExpressionSyntax)context.Node;

        var typeName = creation.Type.ToString();

        if (!IsUiType(typeName))
            return;

        if (creation.ArgumentList is null)
            return;

        foreach (var argument in creation.ArgumentList.Arguments)
        {
            AnalyzeExpression(context, argument.Expression);
        }
    }

    private void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;

        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
            return;

        var methodName = memberAccess.Name.Identifier.Text;

        // Check for Localize() calls (exclusion)
        if (methodName == "Localize" || methodName == "LocalizedString")
            return;

        var className = memberAccess.Expression.ToString();

        // Debug/Trace methods - not user-facing, skip
        if (DebugClassNames.Contains(className))
            return;

        // Check for Console methods (should be localized if shown to users)
        if (className == "Console")
        {
            AnalyzeArguments(context, invocation.ArgumentList);
            return;
        }

        // Check for logging methods (ILogger, etc.)
        if (LoggingMethodNames.Contains(methodName))
        {
            AnalyzeArguments(context, invocation.ArgumentList);
            return;
        }

        // Check for ILogger and similar logging interfaces
        if (className.EndsWith("Logger") || className.EndsWith("Log") ||
            className == "logger" || className == "log")
        {
            AnalyzeArguments(context, invocation.ArgumentList);
        }
    }

    private void AnalyzeArguments(SyntaxNodeAnalysisContext context, ArgumentListSyntax? argumentList)
    {
        if (argumentList is null)
            return;

        foreach (var argument in argumentList.Arguments)
        {
            AnalyzeExpression(context, argument.Expression);
        }
    }

    private void AnalyzeExpression(SyntaxNodeAnalysisContext context, ExpressionSyntax expression)
    {
        if (IsInLocalizeCall(context.Node))
            return;

        if (IsResourceReference(context.Node))
            return;

        if (IsTestCode(context))
            return;

        if (expression is LiteralExpressionSyntax literal &&
            literal.IsKind(SyntaxKind.StringLiteralExpression))
        {
            var text = literal.Token.ValueText;
            var diagnostic = Diagnostic.Create(
                DiagnosticDescriptors.LOC010,
                literal.GetLocation(),
                text);
            context.ReportDiagnostic(diagnostic);
        }
        else if (expression is InterpolatedStringExpressionSyntax interpolated)
        {
            foreach (var content in interpolated.Contents)
            {
                if (content is InterpolatedStringTextSyntax textSyntax)
                {
                    var text = textSyntax.TextToken.ValueText;
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        var diagnostic = Diagnostic.Create(
                            DiagnosticDescriptors.LOC010,
                            content.GetLocation(),
                            text);
                        context.ReportDiagnostic(diagnostic);
                    }
                }
            }
        }
    }

    private static bool IsUiType(string typeName)
    {
        return UiTypeSuffixes.Any(suffix => typeName.Contains(suffix));
    }

    private static bool IsInLocalizeCall(SyntaxNode node)
    {
        var current = node.Parent;
        while (current != null)
        {
            if (current is InvocationExpressionSyntax invocation &&
                invocation.Expression is MemberAccessExpressionSyntax memberAccess)
            {
                var methodName = memberAccess.Name.Identifier.Text;
                if (methodName == "Localize" || methodName == "LocalizedString")
                    return true;
            }
            current = current.Parent;
        }
        return false;
    }

    private static bool IsResourceReference(SyntaxNode node)
    {
        var current = node.Parent;
        while (current != null)
        {
            if (current is MemberAccessExpressionSyntax memberAccess)
            {
                var parentName = memberAccess.Expression.ToString();
                if (parentName is "Strings" or "Resources" or "Translations")
                    return true;
            }
            current = current.Parent;
        }
        return false;
    }

    private static bool IsTestCode(SyntaxNodeAnalysisContext context)
    {
        var tree = context.Node.SyntaxTree;
        var filePath = tree.FilePath;

        if (string.IsNullOrEmpty(filePath))
            return false;

        var fileName = Path.GetFileNameWithoutExtension(filePath);
        if (fileName.Contains("Test", StringComparison.OrdinalIgnoreCase) ||
            fileName.Contains("Spec", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }
}
