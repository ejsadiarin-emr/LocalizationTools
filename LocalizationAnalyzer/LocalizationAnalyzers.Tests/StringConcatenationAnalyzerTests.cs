using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LocalizationAnalyzers.Tests
{
    [TestClass]
    public class StringConcatenationAnalyzerTests
    {
        private static readonly MetadataReference CorlibReference = MetadataReference.CreateFromFile(typeof(object).Assembly.Location);
        private static readonly MetadataReference ConsoleReference = MetadataReference.CreateFromFile(typeof(Console).Assembly.Location);

        private static CSharpCompilation CreateCompilation(string source)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(source);
            return CSharpCompilation.Create(
                "TestAssembly",
                new[] { syntaxTree },
                new[] { CorlibReference, ConsoleReference },
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        }

        [TestMethod]
        public async Task ConcatenationInConsoleWrite_ShouldReportLOC004()
        {
            var source = @"
using System;
class TestClass
{
    void TestMethod()
    {
        string name = ""World"";
        Console.Write(""Hello "" + name);
    }
}";
            var compilation = CreateCompilation(source);
            var analyzer = new Analyzers.StringConcatenationAnalyzer();
            var compWithAnalyzers = compilation.WithAnalyzers(new[] { (Microsoft.CodeAnalysis.Diagnostics.DiagnosticAnalyzer)analyzer }.ToImmutableArray());
            var diagnostics = await compWithAnalyzers.GetAnalyzerDiagnosticsAsync();
            var loc004s = diagnostics.Where(d => d.Id == "LOC004").ToList();
            Assert.AreEqual(1, loc004s.Count);
        }

        [TestMethod]
        public async Task ConcatenationInConsoleWriteLine_ShouldReportLOC004()
        {
            var source = @"
using System;
class TestClass
{
    void TestMethod()
    {
        string name = ""World"";
        Console.WriteLine(""Hello "" + name);
    }
}";
            var compilation = CreateCompilation(source);
            var analyzer = new Analyzers.StringConcatenationAnalyzer();
            var compWithAnalyzers = compilation.WithAnalyzers(new[] { (Microsoft.CodeAnalysis.Diagnostics.DiagnosticAnalyzer)analyzer }.ToImmutableArray());
            var diagnostics = await compWithAnalyzers.GetAnalyzerDiagnosticsAsync();
            var loc004s = diagnostics.Where(d => d.Id == "LOC004").ToList();
            Assert.AreEqual(1, loc004s.Count);
        }

        [TestMethod]
        public async Task ConcatenationInDebugWriteLine_ShouldReportLOC004()
        {
            var source = @"
using System;
using System.Diagnostics;
class TestClass
{
    void TestMethod()
    {
        string name = ""World"";
        Debug.WriteLine(""Hello "" + name);
    }
}";
            var compilation = CreateCompilation(source);
            var analyzer = new Analyzers.StringConcatenationAnalyzer();
            var compWithAnalyzers = compilation.WithAnalyzers(new[] { (Microsoft.CodeAnalysis.Diagnostics.DiagnosticAnalyzer)analyzer }.ToImmutableArray());
            var diagnostics = await compWithAnalyzers.GetAnalyzerDiagnosticsAsync();
            var loc004s = diagnostics.Where(d => d.Id == "LOC004").ToList();
            Assert.AreEqual(1, loc004s.Count);
        }

        [TestMethod]
        public async Task MultipleConcatenationsInOutput_ShouldReportMultiple()
        {
            var source = @"
using System;
class TestClass
{
    void TestMethod()
    {
        string a = ""A"";
        string b = ""B"";
        Console.WriteLine(""First: "" + a);
        Console.WriteLine(""Second: "" + b);
    }
}";
            var compilation = CreateCompilation(source);
            var analyzer = new Analyzers.StringConcatenationAnalyzer();
            var compWithAnalyzers = compilation.WithAnalyzers(new[] { (Microsoft.CodeAnalysis.Diagnostics.DiagnosticAnalyzer)analyzer }.ToImmutableArray());
            var diagnostics = await compWithAnalyzers.GetAnalyzerDiagnosticsAsync();
            var loc004s = diagnostics.Where(d => d.Id == "LOC004").ToList();
            Assert.AreEqual(2, loc004s.Count);
        }

        [TestMethod]
        public async Task ConcatenationOutsideOutputContext_ShouldNotReport()
        {
            var source = @"
class TestClass
{
    void TestMethod()
    {
        string name = ""World"";
        string greeting = ""Hello "" + name;
    }
}";
            var compilation = CreateCompilation(source);
            var analyzer = new Analyzers.StringConcatenationAnalyzer();
            var compWithAnalyzers = compilation.WithAnalyzers(new[] { (Microsoft.CodeAnalysis.Diagnostics.DiagnosticAnalyzer)analyzer }.ToImmutableArray());
            var diagnostics = await compWithAnalyzers.GetAnalyzerDiagnosticsAsync();
            var loc004s = diagnostics.Where(d => d.Id == "LOC004").ToList();
            Assert.AreEqual(0, loc004s.Count);
        }

        [TestMethod]
        public async Task InterpolationInOutputContext_ShouldReportLOC004()
        {
            var source = @"
using System;
class TestClass
{
    void TestMethod()
    {
        string name = ""World"";
        Console.WriteLine($""Hello {name}"");
    }
}";
            var compilation = CreateCompilation(source);
            var analyzer = new Analyzers.StringConcatenationAnalyzer();
            var compWithAnalyzers = compilation.WithAnalyzers(new[] { (Microsoft.CodeAnalysis.Diagnostics.DiagnosticAnalyzer)analyzer }.ToImmutableArray());
            var diagnostics = await compWithAnalyzers.GetAnalyzerDiagnosticsAsync();
            var loc004s = diagnostics.Where(d => d.Id == "LOC004").ToList();
            Assert.AreEqual(1, loc004s.Count);
        }

        [TestMethod]
        public async Task NoStringConcatenation_ShouldNotReport()
        {
            var source = @"
using System;
class TestClass
{
    void TestMethod()
    {
        Console.WriteLine(""Hello World"");
    }
}";
            var compilation = CreateCompilation(source);
            var analyzer = new Analyzers.StringConcatenationAnalyzer();
            var compWithAnalyzers = compilation.WithAnalyzers(new[] { (Microsoft.CodeAnalysis.Diagnostics.DiagnosticAnalyzer)analyzer }.ToImmutableArray());
            var diagnostics = await compWithAnalyzers.GetAnalyzerDiagnosticsAsync();
            var loc004s = diagnostics.Where(d => d.Id == "LOC004").ToList();
            Assert.AreEqual(0, loc004s.Count);
        }
    }
}
