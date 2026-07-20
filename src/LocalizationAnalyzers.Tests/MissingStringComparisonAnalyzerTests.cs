using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LocalizationAnalyzers.Tests
{
    [TestClass]
    public class MissingStringComparisonAnalyzerTests
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
        public async Task ContainsWithoutComparison_ShouldReportLOC006()
        {
            var source = @"
class TestClass
{
    void TestMethod()
    {
        string text = ""Hello World"";
        bool found = text.Contains(""World"");
    }
}";
            var compilation = CreateCompilation(source);
            var analyzer = new Analyzers.MissingStringComparisonAnalyzer();
            var compWithAnalyzers = compilation.WithAnalyzers(new[] { (Microsoft.CodeAnalysis.Diagnostics.DiagnosticAnalyzer)analyzer }.ToImmutableArray());
            var diagnostics = await compWithAnalyzers.GetAnalyzerDiagnosticsAsync();
            var loc006s = diagnostics.Where(d => d.Id == "LOC006").ToList();
            Assert.AreEqual(1, loc006s.Count);
        }

        [TestMethod]
        public async Task StartsWithWithoutComparison_ShouldReportLOC006()
        {
            var source = @"
class TestClass
{
    void TestMethod()
    {
        string text = ""Hello World"";
        bool found = text.StartsWith(""Hello"");
    }
}";
            var compilation = CreateCompilation(source);
            var analyzer = new Analyzers.MissingStringComparisonAnalyzer();
            var compWithAnalyzers = compilation.WithAnalyzers(new[] { (Microsoft.CodeAnalysis.Diagnostics.DiagnosticAnalyzer)analyzer }.ToImmutableArray());
            var diagnostics = await compWithAnalyzers.GetAnalyzerDiagnosticsAsync();
            var loc006s = diagnostics.Where(d => d.Id == "LOC006").ToList();
            Assert.AreEqual(1, loc006s.Count);
        }

        [TestMethod]
        public async Task EndsWithWithoutComparison_ShouldReportLOC006()
        {
            var source = @"
class TestClass
{
    void TestMethod()
    {
        string text = ""Hello World"";
        bool found = text.EndsWith(""World"");
    }
}";
            var compilation = CreateCompilation(source);
            var analyzer = new Analyzers.MissingStringComparisonAnalyzer();
            var compWithAnalyzers = compilation.WithAnalyzers(new[] { (Microsoft.CodeAnalysis.Diagnostics.DiagnosticAnalyzer)analyzer }.ToImmutableArray());
            var diagnostics = await compWithAnalyzers.GetAnalyzerDiagnosticsAsync();
            var loc006s = diagnostics.Where(d => d.Id == "LOC006").ToList();
            Assert.AreEqual(1, loc006s.Count);
        }

        [TestMethod]
        public async Task IndexOfWithoutComparison_ShouldReportLOC006()
        {
            var source = @"
class TestClass
{
    void TestMethod()
    {
        string text = ""Hello World"";
        int index = text.IndexOf(""World"");
    }
}";
            var compilation = CreateCompilation(source);
            var analyzer = new Analyzers.MissingStringComparisonAnalyzer();
            var compWithAnalyzers = compilation.WithAnalyzers(new[] { (Microsoft.CodeAnalysis.Diagnostics.DiagnosticAnalyzer)analyzer }.ToImmutableArray());
            var diagnostics = await compWithAnalyzers.GetAnalyzerDiagnosticsAsync();
            var loc006s = diagnostics.Where(d => d.Id == "LOC006").ToList();
            Assert.AreEqual(1, loc006s.Count);
        }

        [TestMethod]
        public async Task ToLowerWithoutCulture_ShouldReportLOC006()
        {
            var source = @"
class TestClass
{
    void TestMethod()
    {
        string text = ""Hello World"";
        string lower = text.ToLower();
    }
}";
            var compilation = CreateCompilation(source);
            var analyzer = new Analyzers.MissingStringComparisonAnalyzer();
            var compWithAnalyzers = compilation.WithAnalyzers(new[] { (Microsoft.CodeAnalysis.Diagnostics.DiagnosticAnalyzer)analyzer }.ToImmutableArray());
            var diagnostics = await compWithAnalyzers.GetAnalyzerDiagnosticsAsync();
            var loc006s = diagnostics.Where(d => d.Id == "LOC006").ToList();
            Assert.AreEqual(1, loc006s.Count);
        }

        [TestMethod]
        public async Task ContainsWithComparison_ShouldNotReport()
        {
            var source = @"
using System;
class TestClass
{
    void TestMethod()
    {
        string text = ""Hello World"";
        bool found = text.Contains(""World"", StringComparison.Ordinal);
    }
}";
            var compilation = CreateCompilation(source);
            var analyzer = new Analyzers.MissingStringComparisonAnalyzer();
            var compWithAnalyzers = compilation.WithAnalyzers(new[] { (Microsoft.CodeAnalysis.Diagnostics.DiagnosticAnalyzer)analyzer }.ToImmutableArray());
            var diagnostics = await compWithAnalyzers.GetAnalyzerDiagnosticsAsync();
            var loc006s = diagnostics.Where(d => d.Id == "LOC006").ToList();
            Assert.AreEqual(0, loc006s.Count);
        }

        [TestMethod]
        public async Task StartsWithWithComparison_ShouldNotReport()
        {
            var source = @"
using System;
class TestClass
{
    void TestMethod()
    {
        string text = ""Hello World"";
        bool found = text.StartsWith(""Hello"", StringComparison.Ordinal);
    }
}";
            var compilation = CreateCompilation(source);
            var analyzer = new Analyzers.MissingStringComparisonAnalyzer();
            var compWithAnalyzers = compilation.WithAnalyzers(new[] { (Microsoft.CodeAnalysis.Diagnostics.DiagnosticAnalyzer)analyzer }.ToImmutableArray());
            var diagnostics = await compWithAnalyzers.GetAnalyzerDiagnosticsAsync();
            var loc006s = diagnostics.Where(d => d.Id == "LOC006").ToList();
            Assert.AreEqual(0, loc006s.Count);
        }

        [TestMethod]
        public async Task EqualsWithoutComparison_ShouldReportLOC006()
        {
            var source = @"
class TestClass
{
    void TestMethod()
    {
        string text = ""Hello World"";
        bool equal = text.Equals(""Hello"");
    }
}";
            var compilation = CreateCompilation(source);
            var analyzer = new Analyzers.MissingStringComparisonAnalyzer();
            var compWithAnalyzers = compilation.WithAnalyzers(new[] { (Microsoft.CodeAnalysis.Diagnostics.DiagnosticAnalyzer)analyzer }.ToImmutableArray());
            var diagnostics = await compWithAnalyzers.GetAnalyzerDiagnosticsAsync();
            var loc006s = diagnostics.Where(d => d.Id == "LOC006").ToList();
            Assert.AreEqual(1, loc006s.Count);
        }
    }
}
