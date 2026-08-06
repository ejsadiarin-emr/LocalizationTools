using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace LocalizationAnalyzers.Analyzers;

/// <summary>
/// LOC012: Detects hardcoded date/time format strings passed to DateTime/DateTimeOffset methods without CultureInfo.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class HardcodedDateTimeFormatAnalyzer : DiagnosticAnalyzer
{
    private static readonly ImmutableHashSet<string> DateTimeMethodNames =
        ImmutableHashSet.Create("ToString", "ParseExact", "TryParseExact");

    private static readonly ImmutableHashSet<string> StandardFormatSpecifiers =
        ImmutableHashSet.Create(
            "D", "d", "F", "f", "G", "g", "O", "o",
            "R", "r", "S", "s", "T", "t", "U", "u", "Y", "y");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(DiagnosticDescriptors.LOC012);

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
        if (!DateTimeMethodNames.Contains(methodName))
            return;

        if (invocation.ArgumentList.Arguments.Count == 0)
            return;

        var firstArg = invocation.ArgumentList.Arguments[0].Expression;
        if (firstArg is not LiteralExpressionSyntax literal || !literal.IsKind(SyntaxKind.StringLiteralExpression))
            return;

        var formatString = literal.Token.ValueText;

        if (StandardFormatSpecifiers.Contains(formatString))
            return;

        if (!IsDateTimeFormat(formatString))
            return;

        if (HasCultureInfoArgument(invocation))
            return;

        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.LOC012,
            firstArg.GetLocation(),
            formatString));
    }

    private static bool IsDateTimeFormat(string format)
    {
        return format.Contains("dd") ||
               format.Contains("MM") ||
               format.Contains("yyyy") ||
               format.Contains("yy") ||
               format.Contains("hh") ||
               format.Contains("HH") ||
               format.Contains("mm") && format.Contains("ss") ||
               format.Contains("tt") ||
               format.Contains("ddd") ||
               format.Contains("dddd") ||
               format.Contains("MMM") ||
               format.Contains("MMMM") ||
               format.Contains("HH:") ||
               format.Contains("hh:");
    }

    private static bool HasCultureInfoArgument(InvocationExpressionSyntax invocation)
    {
        foreach (var arg in invocation.ArgumentList.Arguments)
        {
            if (arg.Expression is IdentifierNameSyntax id)
            {
                var name = id.Identifier.Text;
                if (name.Contains("Culture") || name.Contains("culture"))
                    return true;
            }

            if (arg.Expression is MemberAccessExpressionSyntax memberAccess)
            {
                var memberName = memberAccess.Name.Identifier.Text;
                if (memberName == "InvariantCulture" || memberName == "CurrentCulture" || memberName == "CurrentUICulture")
                    return true;
            }
        }

        return false;
    }
}
