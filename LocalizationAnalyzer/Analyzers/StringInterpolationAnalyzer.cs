using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace LocalizationAnalyzers.Analyzers;

/// <summary>
/// LOC011: Detects string interpolation in localizable contexts (localizer indexers, UI properties).
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class StringInterpolationAnalyzer : DiagnosticAnalyzer
{
    private static readonly ImmutableHashSet<string> LocalizerIndexerNames =
        ImmutableHashSet.Create("Item", "get_Item");

    private static readonly ImmutableHashSet<string> LocalizerClassNames =
        ImmutableHashSet.Create(
            "StringLocalizer", "IStringLocalizer", "IHtmlLocalizer",
            "HtmlLocalizer", "StringLocalizerOfT");

    private static readonly ImmutableHashSet<string> UiPropertyNames =
        ImmutableHashSet.Create(
            "Text", "Label", "Title", "Caption", "Header",
            "Content", "Placeholder", "ToolTip", "Tooltip",
            "Description", "Message", "Prompt", "Watermark");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(DiagnosticDescriptors.LOC011);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(AnalyzeElementAccess, SyntaxKind.ElementAccessExpression);
        context.RegisterSyntaxNodeAction(AnalyzeAssignment, SyntaxKind.SimpleAssignmentExpression);
        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
    }

    private void AnalyzeElementAccess(SyntaxNodeAnalysisContext context)
    {
        var elementAccess = (ElementAccessExpressionSyntax)context.Node;

        if (!IsLocalizerIndexerAccess(elementAccess))
            return;

        if (elementAccess.ArgumentList.Arguments.Count != 1)
            return;

        var argument = elementAccess.ArgumentList.Arguments[0].Expression;
        if (argument is InterpolatedStringExpressionSyntax)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.LOC011,
                argument.GetLocation(),
                argument.ToString()));
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

        if (assignment.Right is InterpolatedStringExpressionSyntax interpolation)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.LOC011,
                interpolation.GetLocation(),
                interpolation.ToString()));
        }
    }

    private void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;

        // Check for localizer["..."] pattern via indexer-like invocation
        // This handles cases like localizer.Invoke($"...") if needed
        if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
        {
            var methodName = memberAccess.Name.Identifier.Text;
            if (!LocalizerIndexerNames.Contains(methodName))
                return;

            if (!IsLocalizerReceiver(memberAccess.Expression))
                return;

            if (invocation.ArgumentList.Arguments.Count != 1)
                return;

            var argument = invocation.ArgumentList.Arguments[0].Expression;
            if (argument is InterpolatedStringExpressionSyntax)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.LOC011,
                    argument.GetLocation(),
                    argument.ToString()));
            }
        }
    }

    private static bool IsLocalizerIndexerAccess(ElementAccessExpressionSyntax elementAccess)
    {
        if (elementAccess.Expression is not IdentifierNameSyntax identifier)
            return false;

        var name = identifier.Identifier.Text;
        return name.IndexOf("localizer", System.StringComparison.OrdinalIgnoreCase) >= 0
            || name.IndexOf("Localize", System.StringComparison.Ordinal) >= 0;
    }

    private bool IsLocalizerReceiver(ExpressionSyntax expression)
    {
        if (expression is IdentifierNameSyntax identifier)
        {
            var name = identifier.Identifier.Text;
            return name.IndexOf("localizer", System.StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Localize", System.StringComparison.Ordinal) >= 0;
        }

        if (expression is MemberAccessExpressionSyntax memberAccess)
        {
            var typeName = memberAccess.Name.Identifier.Text;
            return LocalizerClassNames.Contains(typeName);
        }

        return false;
    }
}
