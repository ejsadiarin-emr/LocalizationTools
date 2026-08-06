using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace LocalizationAnalyzers.Analyzers;

/// <summary>
/// LOC005: Detects hardcoded date/number format strings.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class HardcodedDateFormatAnalyzer : DiagnosticAnalyzer
{
    private static readonly ImmutableHashSet<string> FormatMethodNames =
        ImmutableHashSet.Create("ToString", "Parse", "TryParse");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(DiagnosticDescriptors.LOC005);

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
        if (!FormatMethodNames.Contains(methodName))
            return;

        if (invocation.ArgumentList.Arguments.Count == 0)
            return;

        var firstArg = invocation.ArgumentList.Arguments[0].Expression;
        if (firstArg is not LiteralExpressionSyntax literal || !literal.IsKind(SyntaxKind.StringLiteralExpression))
            return;

        var formatString = literal.Token.ValueText;
        if (IsDateFormat(formatString) || IsNumberFormat(formatString))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.LOC005,
                firstArg.GetLocation(),
                formatString));
        }
    }

    private static bool IsDateFormat(string format)
    {
        return format.Contains("dd") ||
               format.Contains("MM") ||
               format.Contains("yyyy") ||
               format.Contains("hh") ||
               format.Contains("HH") ||
               format.Contains("mm") && format.Contains("ss") ||
               format.Contains("tt");
    }

    private static bool IsNumberFormat(string format)
    {
        return format.Contains("#,##0") ||
               format.Contains("0.00") ||
               format.Contains("0.0") ||
               format.Contains("#.##") ||
               format.Contains("C2") ||
               format.Contains("N2") ||
               format.Contains("F2");
    }
}
