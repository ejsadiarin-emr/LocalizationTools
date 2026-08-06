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
    public class StringInterpolationAnalyzerTests
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
        public async Task InterpolationInLocalizerIndexer_ShouldReportLOC011()
        {
            var source = @"
class TestClass
{
    void TestMethod()
    {
        string name = ""world"";
        var localizer = new FakeLocalizer();
        var x = localizer[$""Hello {name}""];
    }
}
class FakeLocalizer { public string this[string k] => k; }";
            var compilation = CreateCompilation(source);
            var analyzer = new StringInterpolationAnalyzer();
            var compWithAnalyzers = compilation.WithAnalyzers(new[] { (DiagnosticAnalyzer)analyzer }.ToImmutableArray());
            var diagnostics = await compWithAnalyzers.GetAnalyzerDiagnosticsAsync();
            var loc011s = diagnostics.Where(d => d.Id == "LOC011").ToList();
            Assert.AreEqual(1, loc011s.Count, "Expected LOC011 for interpolation in localizer indexer");
        }

        [TestMethod]
        public async Task InterpolationInUiProperty_ShouldReportLOC011()
        {
            var source = @"
class TestClass
{
    void TestMethod()
    {
        string userName = ""Alice"";
        var label = new Label();
        label.Text = $""Welcome {userName}"";
    }
}
class Label { public string Text { get; set; } }";
            var compilation = CreateCompilation(source);
            var analyzer = new StringInterpolationAnalyzer();
            var compWithAnalyzers = compilation.WithAnalyzers(new[] { (DiagnosticAnalyzer)analyzer }.ToImmutableArray());
            var diagnostics = await compWithAnalyzers.GetAnalyzerDiagnosticsAsync();
            var loc011s = diagnostics.Where(d => d.Id == "LOC011").ToList();
            Assert.AreEqual(1, loc011s.Count, "Expected LOC011 for interpolation in UI property");
        }

        [TestMethod]
        public async Task InterpolationNotInLocalizableContext_ShouldNotReport()
        {
            var source = @"
class TestClass
{
    void TestMethod()
    {
        string name = ""world"";
        var msg = $""Hello {name}"";
        System.Console.WriteLine(msg);
    }
}";
            var compilation = CreateCompilation(source);
            var analyzer = new StringInterpolationAnalyzer();
            var compWithAnalyzers = compilation.WithAnalyzers(new[] { (DiagnosticAnalyzer)analyzer }.ToImmutableArray());
            var diagnostics = await compWithAnalyzers.GetAnalyzerDiagnosticsAsync();
            var loc011s = diagnostics.Where(d => d.Id == "LOC011").ToList();
            Assert.AreEqual(0, loc011s.Count, "Should not report LOC011 for interpolation outside localizable context");
        }

        [TestMethod]
        public async Task FormatStringInLocalizer_ShouldNotReport()
        {
            var source = @"
class TestClass
{
    void TestMethod()
    {
        var localizer = new FakeLocalizer();
        var x = localizer[""Welcome {0}"", ""name""];
    }
}
class FakeLocalizer { public string this[string k, params object[] a] => k; }";
            var compilation = CreateCompilation(source);
            var analyzer = new StringInterpolationAnalyzer();
            var compWithAnalyzers = compilation.WithAnalyzers(new[] { (DiagnosticAnalyzer)analyzer }.ToImmutableArray());
            var diagnostics = await compWithAnalyzers.GetAnalyzerDiagnosticsAsync();
            var loc011s = diagnostics.Where(d => d.Id == "LOC011").ToList();
            Assert.AreEqual(0, loc011s.Count, "Should not report LOC011 for format string in localizer");
        }
    }
}
