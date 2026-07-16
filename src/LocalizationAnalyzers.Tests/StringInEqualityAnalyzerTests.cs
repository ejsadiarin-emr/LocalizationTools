using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LocalizationAnalyzers.Tests
{
    [TestClass]
    public class StringInEqualityAnalyzerTests
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

        private static async Task<List<Diagnostic>> GetDiagnostics(string source, Analyzers.StringInEqualityAnalyzer analyzer)
        {
            var compilation = CreateCompilation(source);
            var compWithAnalyzers = compilation.WithAnalyzers(new[] { (Microsoft.CodeAnalysis.Diagnostics.DiagnosticAnalyzer)analyzer }.ToImmutableArray());
            return (await compWithAnalyzers.GetAnalyzerDiagnosticsAsync()).ToList();
        }

        [TestMethod]
        public async Task EqualsComparisonWithStringLiteral_ShouldReportLOC003()
        {
            var source = @"
class TestClass
{
    void TestMethod()
    {
        string status = ""Running"";
        var result = status == ""Running"";
    }
}";
            var diagnostics = await GetDiagnostics(source, new Analyzers.StringInEqualityAnalyzer());
            var dv003s = diagnostics.Where(d => d.Id == "LOC003").ToList();
            Assert.AreEqual(1, dv003s.Count);
            Assert.IsTrue(dv003s[0].GetMessage().Contains("Running"));
        }

        [TestMethod]
        public async Task EqualsMethodWithStringLiteral_ShouldReportLOC003()
        {
            var source = @"
class TestClass
{
    void TestMethod()
    {
        string status = ""Running"";
        var result = status.Equals(""Running"");
    }
}";
            var diagnostics = await GetDiagnostics(source, new Analyzers.StringInEqualityAnalyzer());
            var dv003s = diagnostics.Where(d => d.Id == "LOC003").ToList();
            Assert.AreEqual(1, dv003s.Count);
        }

        [TestMethod]
        public async Task DictionaryKeyAccess_ShouldReportLOC003()
        {
            var source = @"
class TestClass
{
    void TestMethod()
    {
        var dict = new System.Collections.Generic.Dictionary<string, int>();
        var result = dict[""key""];
    }
}";
            var diagnostics = await GetDiagnostics(source, new Analyzers.StringInEqualityAnalyzer());
            var dv003s = diagnostics.Where(d => d.Id == "LOC003").ToList();
            Assert.AreEqual(1, dv003s.Count);
        }

        [TestMethod]
        public async Task EqualsComparisonInsideIf_ShouldNotReport()
        {
            var source = @"
class TestClass
{
    void TestMethod()
    {
        string status = ""Running"";
        if (status == ""Running"")
        {
        }
    }
}";
            var diagnostics = await GetDiagnostics(source, new Analyzers.StringInEqualityAnalyzer());
            var dv003s = diagnostics.Where(d => d.Id == "LOC003").ToList();
            Assert.AreEqual(0, dv003s.Count);
        }

        [TestMethod]
        public async Task NonStringLiteral_ShouldNotReport()
        {
            var source = @"
class TestClass
{
    void TestMethod()
    {
        string status = ""Running"";
        var result = status == GetStatus();
    }
}";
            var diagnostics = await GetDiagnostics(source, new Analyzers.StringInEqualityAnalyzer());
            var dv003s = diagnostics.Where(d => d.Id == "LOC003").ToList();
            Assert.AreEqual(0, dv003s.Count);
        }
    }
}
