using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LocalizationAnalyzers.Tests
{
    [TestClass]
    public class HardcodedPluralLogicAnalyzerTests
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
        public async Task TernaryWithCountEqualsOne_ShouldReportLOC007()
        {
            var source = @"
using System.Collections.Generic;
class TestClass
{
    void TestMethod()
    {
        var items = new List<string> { ""a"", ""b"" };
        string label = items.Count == 1 ? ""1 item"" : items.Count + "" items"";
    }
}";
            var compilation = CreateCompilation(source);
            var analyzer = new Analyzers.HardcodedPluralLogicAnalyzer();
            var compWithAnalyzers = compilation.WithAnalyzers(new[] { (Microsoft.CodeAnalysis.Diagnostics.DiagnosticAnalyzer)analyzer }.ToImmutableArray());
            var diagnostics = await compWithAnalyzers.GetAnalyzerDiagnosticsAsync();
            var loc007s = diagnostics.Where(d => d.Id == "LOC007").ToList();
            Assert.AreEqual(1, loc007s.Count);
        }

        [TestMethod]
        public async Task TernaryWithCountGreaterThanZero_ShouldReportLOC007()
        {
            var source = @"
using System.Collections.Generic;
class TestClass
{
    void TestMethod()
    {
        var items = new List<string> { ""a"", ""b"" };
        string label = items.Count > 0 ? ""has items"" : ""no items"";
    }
}";
            var compilation = CreateCompilation(source);
            var analyzer = new Analyzers.HardcodedPluralLogicAnalyzer();
            var compWithAnalyzers = compilation.WithAnalyzers(new[] { (Microsoft.CodeAnalysis.Diagnostics.DiagnosticAnalyzer)analyzer }.ToImmutableArray());
            var diagnostics = await compWithAnalyzers.GetAnalyzerDiagnosticsAsync();
            var loc007s = diagnostics.Where(d => d.Id == "LOC007").ToList();
            Assert.AreEqual(1, loc007s.Count);
        }

        [TestMethod]
        public async Task TernaryWithLengthProperty_ShouldReportLOC007()
        {
            var source = @"
class TestClass
{
    void TestMethod()
    {
        string text = ""hello"";
        string label = text.Length == 1 ? ""1 char"" : ""multiple chars"";
    }
}";
            var compilation = CreateCompilation(source);
            var analyzer = new Analyzers.HardcodedPluralLogicAnalyzer();
            var compWithAnalyzers = compilation.WithAnalyzers(new[] { (Microsoft.CodeAnalysis.Diagnostics.DiagnosticAnalyzer)analyzer }.ToImmutableArray());
            var diagnostics = await compWithAnalyzers.GetAnalyzerDiagnosticsAsync();
            var loc007s = diagnostics.Where(d => d.Id == "LOC007").ToList();
            Assert.AreEqual(1, loc007s.Count);
        }

        [TestMethod]
        public async Task TernaryWithCountLessThan_ShouldReportLOC007()
        {
            var source = @"
using System.Collections.Generic;
class TestClass
{
    void TestMethod()
    {
        var items = new List<string> { ""a"", ""b"" };
        string label = items.Count > 1 ? items.Count + "" items"" : ""1 item"";
    }
}";
            var compilation = CreateCompilation(source);
            var analyzer = new Analyzers.HardcodedPluralLogicAnalyzer();
            var compWithAnalyzers = compilation.WithAnalyzers(new[] { (Microsoft.CodeAnalysis.Diagnostics.DiagnosticAnalyzer)analyzer }.ToImmutableArray());
            var diagnostics = await compWithAnalyzers.GetAnalyzerDiagnosticsAsync();
            var loc007s = diagnostics.Where(d => d.Id == "LOC007").ToList();
            Assert.AreEqual(1, loc007s.Count);
        }

        [TestMethod]
        public async Task NoStringLiteralsInBranches_ShouldNotReport()
        {
            var source = @"
using System.Collections.Generic;
class TestClass
{
    void TestMethod()
    {
        var items = new List<string> { ""a"", ""b"" };
        string label = items.Count == 1 ? GetSingleLabel() : GetMultipleLabel();
    }
    string GetSingleLabel() => ""single"";
    string GetMultipleLabel() => ""multiple"";
}";
            var compilation = CreateCompilation(source);
            var analyzer = new Analyzers.HardcodedPluralLogicAnalyzer();
            var compWithAnalyzers = compilation.WithAnalyzers(new[] { (Microsoft.CodeAnalysis.Diagnostics.DiagnosticAnalyzer)analyzer }.ToImmutableArray());
            var diagnostics = await compWithAnalyzers.GetAnalyzerDiagnosticsAsync();
            var loc007s = diagnostics.Where(d => d.Id == "LOC007").ToList();
            Assert.AreEqual(0, loc007s.Count);
        }

        [TestMethod]
        public async Task NoCountComparison_ShouldNotReport()
        {
            var source = @"
class TestClass
{
    void TestMethod()
    {
        int x = 5;
        string label = x == 1 ? ""one"" : ""many"";
    }
}";
            var compilation = CreateCompilation(source);
            var analyzer = new Analyzers.HardcodedPluralLogicAnalyzer();
            var compWithAnalyzers = compilation.WithAnalyzers(new[] { (Microsoft.CodeAnalysis.Diagnostics.DiagnosticAnalyzer)analyzer }.ToImmutableArray());
            var diagnostics = await compWithAnalyzers.GetAnalyzerDiagnosticsAsync();
            var loc007s = diagnostics.Where(d => d.Id == "LOC007").ToList();
            Assert.AreEqual(0, loc007s.Count);
        }

        [TestMethod]
        public async Task TernaryWithStringLiteralCondition_ShouldNotReport()
        {
            var source = @"
class TestClass
{
    void TestMethod()
    {
        string status = ""active"";
        string label = status == ""active"" ? ""Active"" : ""Inactive"";
    }
}";
            var compilation = CreateCompilation(source);
            var analyzer = new Analyzers.HardcodedPluralLogicAnalyzer();
            var compWithAnalyzers = compilation.WithAnalyzers(new[] { (Microsoft.CodeAnalysis.Diagnostics.DiagnosticAnalyzer)analyzer }.ToImmutableArray());
            var diagnostics = await compWithAnalyzers.GetAnalyzerDiagnosticsAsync();
            var loc007s = diagnostics.Where(d => d.Id == "LOC007").ToList();
            Assert.AreEqual(0, loc007s.Count);
        }
    }
}
