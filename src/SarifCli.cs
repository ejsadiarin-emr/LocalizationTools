#if NET8_0_OR_GREATER
#pragma warning disable RS1035 // Do not use banned APIs

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace LocalizationAnalyzers;

/// <summary>
/// CLI entry point for running analyzers and outputting SARIF.
/// Only compiled for net8.0 (not netstandard2.0).
/// </summary>
public static class SarifCli
{
    public static int Main(string[] args)
    {
        if (args.Length < 1)
        {
            Console.Error.WriteLine("Usage: LocalizationAnalyzers <project-path> [output-file]");
            Console.Error.WriteLine("  <project-path>  Directory containing .cs files to analyze");
            Console.Error.WriteLine("  [output-file]   Output SARIF file (default: stdout)");
            return 1;
        }

        var projectPath = args[0];
        var outputFile = args.Length > 1 ? args[1] : null;

        try
        {
            var sarifLog = AnalyzeProject(projectPath);
            var json = JsonSerializer.Serialize(sarifLog, new JsonSerializerOptions { WriteIndented = true });

            if (outputFile != null)
            {
                File.WriteAllText(outputFile, json);
                Console.Error.WriteLine($"SARIF written to: {outputFile}");
            }
            else
            {
                Console.WriteLine(json);
            }

            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }

    private static object AnalyzeProject(string projectPath)
    {
        var projectDir = Path.GetFullPath(projectPath);

        if (File.Exists(projectDir) && projectDir.EndsWith(".csproj"))
        {
            projectDir = Path.GetDirectoryName(projectDir) ?? ".";
        }

        if (!Directory.Exists(projectDir))
        {
            Console.Error.WriteLine($"Directory not found: {projectDir}");
            return CreateEmptySarifLog();
        }

        var csFiles = Directory.GetFiles(projectDir, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains(Path.Combine("bin", "")) &&
                        !f.Contains(Path.Combine("obj", "")) &&
                        !f.Contains("Test"))
            .ToList();

        if (csFiles.Count == 0)
        {
            Console.Error.WriteLine("No .cs files found.");
            return CreateEmptySarifLog();
        }

        var syntaxTrees = csFiles.Select(f =>
            CSharpSyntaxTree.ParseText(File.ReadAllText(f), path: f)).ToList();

        var references = GetMetadataReferences();
        var compilation = CSharpCompilation.Create(
            "AnalysisTarget",
            syntaxTrees,
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var analyzers = GetAnalyzers();
        var compWithAnalyzers = compilation.WithAnalyzers(analyzers.ToImmutableArray());
        var diagnostics = compWithAnalyzers.GetAnalyzerDiagnosticsAsync()
            .GetAwaiter().GetResult();

        return CreateSarifLog(diagnostics, csFiles);
    }

    private static List<MetadataReference> GetMetadataReferences()
    {
        var runtimeDir = System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory();
        return new List<MetadataReference>
        {
            MetadataReference.CreateFromFile(Path.Combine(runtimeDir, "System.Runtime.dll")),
            MetadataReference.CreateFromFile(Path.Combine(runtimeDir, "System.Private.CoreLib.dll")),
            MetadataReference.CreateFromFile(Path.Combine(runtimeDir, "System.Console.dll")),
            MetadataReference.CreateFromFile(Path.Combine(runtimeDir, "netstandard.dll"))
        };
    }

    private static List<DiagnosticAnalyzer> GetAnalyzers()
    {
        var assembly = typeof(SarifCli).Assembly;
        return assembly.GetTypes()
            .Where(t => typeof(DiagnosticAnalyzer).IsAssignableFrom(t) && !t.IsAbstract)
            .Select(t => (DiagnosticAnalyzer)Activator.CreateInstance(t)!)
            .ToList();
    }

    private static object CreateSarifLog(IEnumerable<Diagnostic> diagnostics, List<string> csFiles)
    {
        var results = diagnostics.Select(CreateSarifResult).ToList();
        var artifacts = csFiles.Select(f => new
        {
            location = new { uri = "file:///" + f.Replace("\\", "/") },
            roles = new[] { "resultFile" }
        }).ToList();

        var rules = GetAnalyzers()
            .SelectMany(a => a.SupportedDiagnostics)
            .GroupBy(d => d.Id)
            .Select(g => g.First())
            .Select(d => new
            {
                id = d.Id,
                name = d.Id,
                shortDescription = new { text = d.Title.ToString() },
                fullDescription = new { text = d.Description.ToString() },
                defaultConfiguration = new { level = d.DefaultSeverity.ToString().ToLower() },
                properties = new { category = d.Category }
            })
            .ToList();

        return new
        {
            schema = "https://json.schemastore.org/sarif-2.1.0.json",
            version = "2.1.0",
            runs = new[]
            {
                new
                {
                    tool = new
                    {
                        driver = new
                        {
                            name = "LocalizationAnalyzers",
                            version = "1.0.0",
                            informationUri = "https://github.com/your-org/LocalizationAnalyzers",
                            rules = rules
                        }
                    },
                    results = results,
                    artifacts = artifacts
                }
            }
        };
    }

    private static object CreateSarifResult(Diagnostic diagnostic)
    {
        var location = diagnostic.Location;
        var span = location.GetLineSpan();

        return new
        {
            ruleId = diagnostic.Id,
            level = diagnostic.Severity switch
            {
                DiagnosticSeverity.Error => "error",
                DiagnosticSeverity.Warning => "warning",
                DiagnosticSeverity.Info => "note",
                _ => "warning"
            },
            message = new { text = diagnostic.GetMessage() },
            locations = new[]
            {
                new
                {
                    physicalLocation = new
                    {
                        artifactLocation = new
                        {
                            uri = "file:///" + (location.SourceTree?.FilePath.Replace("\\", "/") ?? "unknown")
                        },
                        region = new
                        {
                            startLine = span.StartLinePosition.Line + 1,
                            startColumn = span.StartLinePosition.Character + 1,
                            endLine = span.EndLinePosition.Line + 1,
                            endColumn = span.EndLinePosition.Character + 1
                        }
                    }
                }
            }
        };
    }

    private static object CreateEmptySarifLog()
    {
        return new
        {
            schema = "https://json.schemastore.org/sarif-2.1.0.json",
            version = "2.1.0",
            runs = Array.Empty<object>()
        };
    }
}
#pragma warning restore RS1035
#endif
