using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Simplification;

namespace LocalizationAnalyzers.CodeFixes;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ExtractToResourceCodeFix))]
[Shared]
public class ExtractToResourceCodeFix : CodeFixProvider
{
    private const string Title = "Extract to Localize()";

    public override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(DiagnosticDescriptors.LOC010.Id);

    public override FixAllProvider GetFixAllProvider() =>
        WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken)
            .ConfigureAwait(false);

        if (root is null)
            return;

        var diagnostic = context.Diagnostics[0];
        var diagnosticSpan = diagnostic.Location.SourceSpan;

        var node = root.FindNode(diagnosticSpan);
        if (node is null)
            return;

        context.RegisterCodeFix(
            CodeAction.Create(
                title: Title,
                createChangedDocument: ct => ExtractToLocalizeAsync(context.Document, node, ct),
                equivalenceKey: Title),
            diagnostic);
    }

    private async Task<Document> ExtractToLocalizeAsync(
        Document document,
        SyntaxNode node,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root is null)
            return document;

        var (classDeclaration, methodDeclaration) = GetEnclosingTypeAndMethod(node);

        string key;
        string? value = null;

        if (node is LiteralExpressionSyntax literal &&
            literal.IsKind(SyntaxKind.StringLiteralExpression))
        {
            value = literal.Token.ValueText;
            key = GenerateKey(classDeclaration, methodDeclaration, value);
        }
        else if (node is InterpolatedStringExpressionSyntax interpolated)
        {
            key = GenerateKeyFromInterpolation(classDeclaration, methodDeclaration, interpolated);
            value = ExtractInterpolationText(interpolated);
        }
        else
        {
            return document;
        }

        var localizeCall = CreateLocalizeCall(key, node);
        var newRoot = root.ReplaceNode(node, localizeCall);

        if (value is not null)
        {
            newRoot = AddToResourceFile(document, key, value, newRoot);
        }

        return document.WithSyntaxRoot(newRoot);
    }

    private (ClassDeclarationSyntax? classDeclaration, MethodDeclarationSyntax? methodDeclaration)
        GetEnclosingTypeAndMethod(SyntaxNode node)
    {
        ClassDeclarationSyntax? classDeclaration = null;
        MethodDeclarationSyntax? methodDeclaration = null;

        var current = node.Parent;
        while (current != null)
        {
            if (current is ClassDeclarationSyntax classDecl)
                classDeclaration = classDecl;
            else if (current is MethodDeclarationSyntax methodDecl)
                methodDeclaration = methodDecl;

            current = current.Parent;
        }

        return (classDeclaration, methodDeclaration);
    }

    public string GenerateKey(
        ClassDeclarationSyntax? classDeclaration,
        MethodDeclarationSyntax? methodDeclaration,
        string value)
    {
        var className = classDeclaration?.Identifier.Text ?? "Unknown";
        var methodName = methodDeclaration?.Identifier.Text ?? "Unknown";
        var slug = Slugify(value);
        return $"{className}.{methodName}.{slug}";
    }

    private static string GenerateKeyFromInterpolation(
        ClassDeclarationSyntax? classDeclaration,
        MethodDeclarationSyntax? methodDeclaration,
        InterpolatedStringExpressionSyntax interpolated)
    {
        var className = classDeclaration?.Identifier.Text ?? "Unknown";
        var methodName = methodDeclaration?.Identifier.Text ?? "Unknown";

        var textParts = string.Concat(interpolated.Contents
            .OfType<InterpolatedStringTextSyntax>()
            .Select(t => t.TextToken.ValueText));

        var slug = Slugify(textParts);
        return $"{className}.{methodName}.{slug}";
    }

    private static string Slugify(string value)
    {
        var slugified = Regex.Replace(value, @"[^a-zA-Z0-9]", "")
            .ToLowerInvariant();

        if (slugified.Length > 50)
            slugified = slugified.Substring(0, 50);

        return slugified;
    }

    private static ExpressionSyntax CreateLocalizeCall(string key, SyntaxNode originalNode)
    {
        var keyLiteral = SyntaxFactory.LiteralExpression(
            SyntaxKind.StringLiteralExpression,
            SyntaxFactory.Literal(key));

        var localizeCall = SyntaxFactory.InvocationExpression(
            SyntaxFactory.IdentifierName("Localize"))
            .WithArgumentList(
                SyntaxFactory.ArgumentList(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.Argument(keyLiteral))));

        return localizeCall.WithTriviaFrom(originalNode);
    }

    private static string ExtractInterpolationText(InterpolatedStringExpressionSyntax interpolated)
    {
        return string.Concat(interpolated.Contents
            .OfType<InterpolatedStringTextSyntax>()
            .Select(t => t.TextToken.ValueText));
    }

#pragma warning disable RS1035 // Suppress file IO ban — this is a CodeFixProvider, not an analyzer
    private SyntaxNode AddToResourceFile(
        Document document,
        string key,
        string value,
        SyntaxNode newRoot)
    {
        try
        {
            var project = document.Project;
            var projectDir = Path.GetDirectoryName(project.FilePath);

            if (projectDir is null)
                return newRoot;

            var resourcePath = Path.Combine(projectDir, "Resources", "en.json");

            var existingEntries = new Dictionary<string, string>();

            if (File.Exists(resourcePath))
            {
                var json = File.ReadAllText(resourcePath);
                existingEntries = ParseSimpleJson(json);
            }

            var finalKey = key;
            var counter = 2;
            while (existingEntries.ContainsKey(finalKey))
            {
                finalKey = $"{key}{counter}";
                counter++;
            }

            existingEntries[finalKey] = value;

            var dir = Path.GetDirectoryName(resourcePath);
            if (dir is not null && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var output = WriteSimpleJson(existingEntries);
            File.WriteAllText(resourcePath, output);
        }
        catch
        {
            // If resource file update fails, continue with Localize() replacement
        }

        return newRoot;
    }
#pragma warning restore RS1035

    private static Dictionary<string, string> ParseSimpleJson(string json)
    {
        var result = new Dictionary<string, string>();
        if (string.IsNullOrWhiteSpace(json))
            return result;

        var lines = json.Split('\n');
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("\"") && trimmed.Contains(":"))
            {
                var colonIndex = trimmed.IndexOf(':');
                var keyPart = trimmed.Substring(0, colonIndex).Trim().Trim('"');
                var valuePart = trimmed.Substring(colonIndex + 1).Trim().TrimEnd(',').Trim('"');

                if (!string.IsNullOrEmpty(keyPart))
                    result[keyPart] = valuePart;
            }
        }

        return result;
    }

    private static string WriteSimpleJson(Dictionary<string, string> entries)
    {
        var sb = new StringBuilder();
        sb.AppendLine("{");

        var index = 0;
        foreach (var entry in entries)
        {
            var comma = index < entries.Count - 1 ? "," : "";
            sb.AppendLine($"  \"{entry.Key}\": \"{EscapeJsonString(entry.Value)}\"{comma}");
            index++;
        }

        sb.AppendLine("}");
        return sb.ToString();
    }

    private static string EscapeJsonString(string value)
    {
        return value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r")
            .Replace("\t", "\\t");
    }
}
