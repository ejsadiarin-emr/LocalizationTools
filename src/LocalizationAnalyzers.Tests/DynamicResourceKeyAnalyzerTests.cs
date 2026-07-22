using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using LocalizationAnalyzers.Analyzers;

namespace LocalizationAnalyzers.Tests
{
    [TestClass]
    public class DynamicResourceKeyAnalyzerTests
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
        public async Task InterpolatedResourceKey_ShouldReportLOC013()
        {
            var source = @"
class TestClass
{
    void TestMethod()
    {
        var localizer = new FakeLocalizer();
        string errorCode = ""404"";
        var x = localizer[$""Error_{errorCode}""];
    }
}
class FakeLocalizer { public string this[string k] => k; }";
            var compilation = CreateCompilation(source);
            var analyzer = new DynamicResourceKeyAnalyzer();
            var compWithAnalyzers = compilation.WithAnalyzers(new[] { (DiagnosticAnalyzer)analyzer }.ToImmutableArray());
            var diagnostics = await compWithAnalyzers.GetAnalyzerDiagnosticsAsync();
            var loc013s = diagnostics.Where(d => d.Id == "LOC013").ToList();
            Assert.AreEqual(1, loc013s.Count, "Expected LOC013 for interpolated resource key");
        }

        [TestMethod]
        public async Task ConcatenatedResourceKey_ShouldReportLOC013()
        {
            var source = @"
class TestClass
{
    void TestMethod()
    {
        var localizer = new FakeLocalizer();
        string code = ""404"";
        var x = localizer[""Error_"" + code];
    }
}
class FakeLocalizer { public string this[string k] => k; }";
            var compilation = CreateCompilation(source);
            var analyzer = new DynamicResourceKeyAnalyzer();
            var compWithAnalyzers = compilation.WithAnalyzers(new[] { (DiagnosticAnalyzer)analyzer }.ToImmutableArray());
            var diagnostics = await compWithAnalyzers.GetAnalyzerDiagnosticsAsync();
            var loc013s = diagnostics.Where(d => d.Id == "LOC013").ToList();
            Assert.AreEqual(1, loc013s.Count, "Expected LOC013 for concatenated resource key");
        }

        [TestMethod]
        public async Task LiteralResourceKey_ShouldNotReport()
        {
            var source = @"
class TestClass
{
    void TestMethod()
    {
        var localizer = new FakeLocalizer();
        var x = localizer[""Error_NotFound""];
    }
}
class FakeLocalizer { public string this[string k] => k; }";
            var compilation = CreateCompilation(source);
            var analyzer = new DynamicResourceKeyAnalyzer();
            var compWithAnalyzers = compilation.WithAnalyzers(new[] { (DiagnosticAnalyzer)analyzer }.ToImmutableArray());
            var diagnostics = await compWithAnalyzers.GetAnalyzerDiagnosticsAsync();
            var loc013s = diagnostics.Where(d => d.Id == "LOC013").ToList();
            Assert.AreEqual(0, loc013s.Count, "Should not report LOC013 for literal resource key");
        }

        [TestMethod]
        public async Task ConstantResourceKey_ShouldNotReport()
        {
            var source = @"
class TestClass
{
    void TestMethod()
    {
        var localizer = new FakeLocalizer();
        var x = localizer[ResourceKeys.ErrorNotFound];
    }
}
class ResourceKeys { public const string ErrorNotFound = ""Error_NotFound""; }
class FakeLocalizer { public string this[string k] => k; }";
            var compilation = CreateCompilation(source);
            var analyzer = new DynamicResourceKeyAnalyzer();
            var compWithAnalyzers = compilation.WithAnalyzers(new[] { (DiagnosticAnalyzer)analyzer }.ToImmutableArray());
            var diagnostics = await compWithAnalyzers.GetAnalyzerDiagnosticsAsync();
            var loc013s = diagnostics.Where(d => d.Id == "LOC013").ToList();
            Assert.AreEqual(0, loc013s.Count, "Should not report LOC013 for constant resource key");
        }

        [TestMethod]
        public async Task VariableResourceKey_ShouldReportLOC013()
        {
            var source = @"
class TestClass
{
    void TestMethod()
    {
        var localizer = new FakeLocalizer();
        string key = ""Error_NotFound"";
        var x = localizer[key];
    }
}
class FakeLocalizer { public string this[string k] => k; }";
            var compilation = CreateCompilation(source);
            var analyzer = new DynamicResourceKeyAnalyzer();
            var compWithAnalyzers = compilation.WithAnalyzers(new[] { (DiagnosticAnalyzer)analyzer }.ToImmutableArray());
            var diagnostics = await compWithAnalyzers.GetAnalyzerDiagnosticsAsync();
            var loc013s = diagnostics.Where(d => d.Id == "LOC013").ToList();
            Assert.AreEqual(1, loc013s.Count, "Expected LOC013 for variable resource key");
        }
    }
}
