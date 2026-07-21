#if NET10_0_OR_GREATER
#pragma warning disable RS1035 // Do not use banned APIs

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
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
/// Only compiled for net10.0 (not netstandard2.0).
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
            Console.Error.WriteLine("");
            Console.Error.WriteLine("Output includes SARIF 2.1.0 with execution metrics:");
            Console.Error.WriteLine("  - invocations[]: overall start/end time, arguments, working directory");
            Console.Error.WriteLine("  - properties.fileMetrics[]: per-file timing, size, line count, diagnostic count");
            Console.Error.WriteLine("  - properties.totalFileCount/totalLineCount/totalDurationMs: summary stats");
            return 1;
        }

        var projectPath = args[0];
        var outputFile = args.Length > 1 ? args[1] : null;

        try
        {
            var sarifLog = AnalyzeProject(projectPath, args);
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

    public static object AnalyzeProject(string projectPath, string[] cliArgs)
    {
        var overallStopwatch = Stopwatch.StartNew();
        var overallStartTime = DateTime.UtcNow;

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

        var excludedDirs = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "bin", "obj", "Test", "TestResults"
        };

        var csFiles = Directory.GetFiles(projectDir, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                          .Any(part => excludedDirs.Contains(part)))
            .ToList();

        if (csFiles.Count == 0)
        {
            Console.Error.WriteLine("No .cs files found.");
            return CreateEmptySarifLog();
        }

        var fileDataList = new List<(string FilePath, DateTime StartTime, DateTime EndTime, long FileSizeBytes, int LineCount)>();
        var syntaxTrees = new List<SyntaxTree>();
        var totalLineCount = 0;

        foreach (var filePath in csFiles)
        {
            var fileStopwatch = Stopwatch.StartNew();
            var fileStartTime = DateTime.UtcNow;

            var content = File.ReadAllText(filePath);
            var fileInfo = new FileInfo(filePath);
            var lineCount = content.Split('\n').Length;
            totalLineCount += lineCount;

            var syntaxTree = CSharpSyntaxTree.ParseText(content, path: filePath);
            syntaxTrees.Add(syntaxTree);

            fileStopwatch.Stop();
            fileDataList.Add((filePath.Replace("\\", "/"), fileStartTime, fileStartTime + fileStopwatch.Elapsed, fileInfo.Length, lineCount));
        }

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

        overallStopwatch.Stop();

        var diagnosticsByFile = diagnostics.GroupBy(d => d.Location.SourceTree?.FilePath ?? "unknown")
            .ToDictionary(g => g.Key, g => g.Count());

        var fileMetrics = fileDataList.Select(fd => new
        {
            filePath = fd.FilePath,
            startTimeUtc = fd.StartTime,
            endTimeUtc = fd.EndTime,
            fileSizeBytes = fd.FileSizeBytes,
            lineCount = fd.LineCount,
            diagnosticCount = diagnosticsByFile.GetValueOrDefault(fd.FilePath, 0)
        }).ToList<object>();

        return CreateSarifLog(diagnostics, csFiles, overallStartTime, DateTime.UtcNow,
            overallStopwatch.ElapsedMilliseconds, cliArgs, projectDir,
            fileMetrics, csFiles.Count, totalLineCount);
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

    private static object CreateSarifLog(IEnumerable<Diagnostic> diagnostics, List<string> csFiles,
        DateTime startTimeUtc, DateTime endTimeUtc, long totalDurationMs,
        string[] cliArgs, string workingDirectory,
        List<object> fileMetrics, int totalFileCount, int totalLineCount)
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
                defaultConfiguration = new { level = d.DefaultSeverity switch
                {
                    DiagnosticSeverity.Error => "error",
                    DiagnosticSeverity.Warning => "warning",
                    DiagnosticSeverity.Info => "note",
                    _ => "warning"
                } },
                properties = new { category = d.Category }
            })
            .ToList();

        return new
        {
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
                    invocations = new[]
                    {
                        new
                        {
                            startTimeUtc = startTimeUtc,
                            endTimeUtc = endTimeUtc,
                            executionSuccessful = true,
                            arguments = cliArgs,
                            workingDirectory = workingDirectory
                        }
                    },
                    results = results,
                    artifacts = artifacts,
                    properties = new
                    {
                        fileMetrics = fileMetrics,
                        totalFileCount = totalFileCount,
                        totalLineCount = totalLineCount,
                        totalDurationMs = totalDurationMs
                    }
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
                            version = "1.0.0"
                        }
                    },
                    invocations = Array.Empty<object>(),
                    results = Array.Empty<object>(),
                    properties = new
                    {
                        fileMetrics = Array.Empty<object>(),
                        totalFileCount = 0,
                        totalLineCount = 0,
                        totalDurationMs = 0
                    }
                }
            }
        };
    }
}
#pragma warning restore RS1035
#endif
