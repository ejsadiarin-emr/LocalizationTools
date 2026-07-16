using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace LocalizationAnalyzers.Analyzers;

/// <summary>
/// LOC002: Detects string literals passed to data-access or lookup methods.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class StringInDataAccessAnalyzer : DiagnosticAnalyzer
{
    private static readonly ImmutableHashSet<string> DataAccessMethodNames =
        ImmutableHashSet.Create(
            "Find", "Get", "Query", "Lookup", "Search",
            "FindFirst", "FindLast", "FindOne", "FindAll",
            "GetOne", "GetMany", "GetAll",
            "QueryOne", "QueryMany");

    private static readonly ImmutableHashSet<string> DatabaseContextNames =
        ImmutableHashSet.Create(
            "Db", "Database", "Context", "Repository",
            "DbContext", "DataContext", "UnitOfWork");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(DiagnosticDescriptors.LOC002);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
    }

    private void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;

        string methodName;

        if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
        {
            methodName = memberAccess.Name.Identifier.Text;
        }
        else if (invocation.Expression is IdentifierNameSyntax identifierName)
        {
            methodName = identifierName.Identifier.Text;
        }
        else
        {
            return;
        }

        if (!IsDataAccessMethod(methodName))
            return;

        if (invocation.ArgumentList is null)
            return;

        foreach (var argument in invocation.ArgumentList.Arguments)
        {
            if (argument.Expression is LiteralExpressionSyntax literal &&
                literal.IsKind(SyntaxKind.StringLiteralExpression))
            {
                var text = literal.Token.ValueText;
                var diagnostic = Diagnostic.Create(
                    DiagnosticDescriptors.LOC002,
                    literal.GetLocation(),
                    text);
                context.ReportDiagnostic(diagnostic);
            }
        }
    }

    private static bool IsDataAccessMethod(string methodName)
    {
        if (DataAccessMethodNames.Contains(methodName))
            return true;

        foreach (var prefix in DatabaseContextNames)
        {
            if (methodName.StartsWith(prefix))
                return true;
        }

        return false;
    }
}
