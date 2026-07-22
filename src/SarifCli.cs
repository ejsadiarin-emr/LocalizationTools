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
            Console.Error.WriteLine("Usage: LocalizationAnalyzers <project-path> [output-file] [--with-ca-rules]");
            Console.Error.WriteLine("  <project-path>      Directory containing .cs files to analyze");
            Console.Error.WriteLine("  [output-file]       Output SARIF file (default: stdout)");
            Console.Error.WriteLine("  --with-ca-rules     Include built-in CA globalization rules (CA1303-CA1311)");
            Console.Error.WriteLine("");
            Console.Error.WriteLine("Output includes SARIF 2.1.0 with execution metrics:");
            Console.Error.WriteLine("  - invocations[]: overall start/end time, arguments, working directory");
            Console.Error.WriteLine("  - properties.fileMetrics[]: per-file timing, size, line count, diagnostic count");
            Console.Error.WriteLine("  - properties.totalFileCount/totalLineCount/totalDurationMs: summary stats");
            return 1;
        }

        var includeCaRules = args.Any(a => a.Equals("--with-ca-rules", StringComparison.OrdinalIgnoreCase));
        var filteredArgs = args.Where(a => !a.Equals("--with-ca-rules", StringComparison.OrdinalIgnoreCase)).ToArray();

        var projectPath = filteredArgs[0];
        var outputFile = filteredArgs.Length > 1 ? filteredArgs[1] : null;

        try
        {
            var sarifLog = AnalyzeProject(projectPath, args, includeCaRules);
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

    public static object AnalyzeProject(string projectPath, string[] cliArgs, bool includeCaRules = false)
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

        var analyzers = GetAnalyzers(includeCaRules);
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
            fileMetrics, csFiles.Count, totalLineCount, includeCaRules);
    }

    private static List<MetadataReference> GetMetadataReferences()
    {
        var runtimeDir = System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory();
        var references = new List<MetadataReference>
        {
            MetadataReference.CreateFromFile(Path.Combine(runtimeDir, "System.Runtime.dll")),
            MetadataReference.CreateFromFile(Path.Combine(runtimeDir, "System.Private.CoreLib.dll")),
            MetadataReference.CreateFromFile(Path.Combine(runtimeDir, "System.Console.dll")),
            MetadataReference.CreateFromFile(Path.Combine(runtimeDir, "netstandard.dll")),
            MetadataReference.CreateFromFile(Path.Combine(runtimeDir, "System.Linq.dll")),
            MetadataReference.CreateFromFile(Path.Combine(runtimeDir, "System.Globalization.dll")),
            MetadataReference.CreateFromFile(Path.Combine(runtimeDir, "System.Collections.Immutable.dll")),
            MetadataReference.CreateFromFile(Path.Combine(runtimeDir, "System.Memory.dll")),
            MetadataReference.CreateFromFile(Path.Combine(runtimeDir, "System.Text.RegularExpressions.dll"))
        };

        // Add Roslyn assemblies for CA rules that need semantic analysis
        var codeAnalysisDir = Path.GetDirectoryName(typeof(CSharpCompilation).Assembly.Location);
        if (codeAnalysisDir != null)
        {
            var codeAnalysisDll = Path.Combine(codeAnalysisDir, "Microsoft.CodeAnalysis.dll");
            var codeAnalysisCSharpDll = Path.Combine(codeAnalysisDir, "Microsoft.CodeAnalysis.CSharp.dll");
            if (File.Exists(codeAnalysisDll))
                references.Add(MetadataReference.CreateFromFile(codeAnalysisDll));
            if (File.Exists(codeAnalysisCSharpDll))
                references.Add(MetadataReference.CreateFromFile(codeAnalysisCSharpDll));
        }

        return references;
    }

    private static List<DiagnosticAnalyzer> GetAnalyzers(bool includeCaRules = false)
    {
        var assembly = typeof(SarifCli).Assembly;
        var analyzers = assembly.GetTypes()
            .Where(t => typeof(DiagnosticAnalyzer).IsAssignableFrom(t) && !t.IsAbstract)
            .Select(t => (DiagnosticAnalyzer)Activator.CreateInstance(t)!)
            .ToList();

        if (includeCaRules)
        {
            analyzers.AddRange(GetCaAnalyzers());
        }

        return analyzers;
    }

    private static IEnumerable<DiagnosticAnalyzer> GetCaAnalyzers()
    {
        var analyzerDlls = FindCaAnalyzerDlls();
        if (analyzerDlls.Count == 0)
        {
            yield break;
        }

        // Register assembly resolve handler to redirect Microsoft.CodeAnalysis v3.11.0
        // requests to the version already loaded in the current process (v5.6.0+).
        ResolveEventHandler? resolveHandler = null;
        resolveHandler = (sender, args) =>
        {
            var requestedName = new AssemblyName(args.Name);
            if (requestedName.Name == "Microsoft.CodeAnalysis" ||
                requestedName.Name == "Microsoft.CodeAnalysis.CSharp" ||
                requestedName.Name == "Microsoft.CodeAnalysis.CSharp.Workspaces")
            {
                return AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == requestedName.Name);
            }
            return null;
        };
        AppDomain.CurrentDomain.AssemblyResolve += resolveHandler;

        try
        {
            foreach (var dllPath in analyzerDlls)
            {
                Assembly asm;
                try
                {
                    asm = Assembly.LoadFrom(dllPath);
                }
                catch
                {
                    continue;
                }

                DiagnosticAnalyzer[]? analyzers = null;
                try
                {
                    analyzers = asm.GetTypes()
                        .Where(t => typeof(DiagnosticAnalyzer).IsAssignableFrom(t) && !t.IsAbstract)
                        .Select(t => (DiagnosticAnalyzer)Activator.CreateInstance(t)!)
                        .ToArray();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    analyzers = ex.Types
                        .Where(t => t != null && typeof(DiagnosticAnalyzer).IsAssignableFrom(t) && !t.IsAbstract)
                        .Select(t => (DiagnosticAnalyzer)Activator.CreateInstance(t!)!)
                        .ToArray();
                }
                catch
                {
                    // Skip assemblies that can't be loaded
                }

                if (analyzers != null)
                {
                    foreach (var analyzer in analyzers)
                    {
                        yield return analyzer;
                    }
                }
            }
        }
        finally
        {
            AppDomain.CurrentDomain.AssemblyResolve -= resolveHandler;
        }
    }

    private static List<string> FindCaAnalyzerDlls()
    {
        var dlls = new List<string>();

        // Search in NuGet global packages folder
        var nugetPackages = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".nuget", "packages");

        var analyzerPackageDir = Path.Combine(nugetPackages, "microsoft.codeanalysis.netanalyzers");
        if (!Directory.Exists(analyzerPackageDir))
        {
            return dlls;
        }

        // Find latest version directory
        var versionDirs = Directory.GetDirectories(analyzerPackageDir)
            .Select(d => new DirectoryInfo(d))
            .OrderByDescending(d => d.Name)
            .ToList();

        if (versionDirs.Count == 0)
        {
            return dlls;
        }

        var latestVersionDir = versionDirs[0].FullName;
        var analyzersDir = Path.Combine(latestVersionDir, "analyzers", "dotnet");

        // Main analyzer assembly
        var mainDll = Path.Combine(analyzersDir, "Microsoft.CodeAnalysis.NetAnalyzers.dll");
        if (File.Exists(mainDll))
        {
            dlls.Add(mainDll);
        }

        // C#-specific analyzer assembly (contains CA1305, CA1307, CA1308, CA1309, CA1310, CA1311)
        var csDll = Path.Combine(analyzersDir, "cs", "Microsoft.CodeAnalysis.CSharp.NetAnalyzers.dll");
        if (File.Exists(csDll))
        {
            dlls.Add(csDll);
        }

        return dlls;
    }

    private static object CreateSarifLog(IEnumerable<Diagnostic> diagnostics, List<string> csFiles,
        DateTime startTimeUtc, DateTime endTimeUtc, long totalDurationMs,
        string[] cliArgs, string workingDirectory,
        List<object> fileMetrics, int totalFileCount, int totalLineCount,
        bool includeCaRules = false)
    {
        var results = diagnostics.Select(CreateSarifResult).ToList();
        var artifacts = csFiles.Select(f => new
        {
            location = new { uri = "file:///" + f.Replace("\\", "/") },
            roles = new[] { "resultFile" }
        }).ToList();

        var rules = GetAnalyzers(includeCaRules)
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

        var driver = new
        {
            name = "LocalizationAnalyzers",
            version = "1.0.0",
            informationUri = "https://github.com/your-org/LocalizationAnalyzers",
            rules = rules
        };

        object toolObject;
        if (includeCaRules)
        {
            toolObject = new
            {
                driver = driver,
                components = new[]
                {
                    new
                    {
                        name = "Microsoft.CodeAnalysis.NetAnalyzers",
                        version = "10.0.302",
                        informationUri = "https://github.com/dotnet/roslyn-analyzers"
                    }
                }
            };
        }
        else
        {
            toolObject = new
            {
                driver = driver
            };
        }

        return new
        {
            version = "2.1.0",
            runs = new[]
            {
                new
                {
                    tool = toolObject,
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
