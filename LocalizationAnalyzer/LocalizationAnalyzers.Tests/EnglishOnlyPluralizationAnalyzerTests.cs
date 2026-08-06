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
    public class EnglishOnlyPluralizationAnalyzerTests
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
        public async Task IfElsePluralization_ShouldReportLOC014()
        {
            var source = @"
class TestClass
{
    void TestMethod()
    {
        int count = 5;
        string text;
        if (count == 1) { text = ""item""; } else { text = ""items""; }
    }
}";
            var compilation = CreateCompilation(source);
            var analyzer = new EnglishOnlyPluralizationAnalyzer();
            var compWithAnalyzers = compilation.WithAnalyzers(new[] { (DiagnosticAnalyzer)analyzer }.ToImmutableArray());
            var diagnostics = await compWithAnalyzers.GetAnalyzerDiagnosticsAsync();
            var loc014s = diagnostics.Where(d => d.Id == "LOC014").ToList();
            Assert.AreEqual(1, loc014s.Count, "Expected LOC014 for if/else pluralization");
        }

        [TestMethod]
        public async Task ConcatenationWithCountInUiContext_ShouldReportLOC014()
        {
            var source = @"
class TestClass
{
    void TestMethod()
    {
        var label = new Label();
        int count = 5;
        label.Text = ""You have "" + count + "" items"";
    }
}
class Label { public string Text { get; set; } }";
            var compilation = CreateCompilation(source);
            var analyzer = new EnglishOnlyPluralizationAnalyzer();
            var compWithAnalyzers = compilation.WithAnalyzers(new[] { (DiagnosticAnalyzer)analyzer }.ToImmutableArray());
            var diagnostics = await compWithAnalyzers.GetAnalyzerDiagnosticsAsync();
            var loc014s = diagnostics.Where(d => d.Id == "LOC014").ToList();
            Assert.AreEqual(1, loc014s.Count, "Expected LOC014 for concatenation with count in UI");
        }

        [TestMethod]
        public async Task NoPluralization_ShouldNotReport()
        {
            var source = @"
class TestClass
{
    void TestMethod()
    {
        var items = new int[5];
        var count = items.Length;
    }
}";
            var compilation = CreateCompilation(source);
            var analyzer = new EnglishOnlyPluralizationAnalyzer();
            var compWithAnalyzers = compilation.WithAnalyzers(new[] { (DiagnosticAnalyzer)analyzer }.ToImmutableArray());
            var diagnostics = await compWithAnalyzers.GetAnalyzerDiagnosticsAsync();
            var loc014s = diagnostics.Where(d => d.Id == "LOC014").ToList();
            Assert.AreEqual(0, loc014s.Count, "Should not report LOC014 when no pluralization detected");
        }

        [TestMethod]
        public async Task TernaryPluralization_ShouldReportLOC014()
        {
            var source = @"
class TestClass
{
    void TestMethod()
    {
        int count = 5;
        string text = count == 1 ? ""item"" : ""items"";
    }
}";
            var compilation = CreateCompilation(source);
            var analyzer = new EnglishOnlyPluralizationAnalyzer();
            var compWithAnalyzers = compilation.WithAnalyzers(new[] { (DiagnosticAnalyzer)analyzer }.ToImmutableArray());
            var diagnostics = await compWithAnalyzers.GetAnalyzerDiagnosticsAsync();
            var loc014s = diagnostics.Where(d => d.Id == "LOC014").ToList();
            Assert.AreEqual(1, loc014s.Count, "Expected LOC014 for ternary pluralization");
        }

        [TestMethod]
        public async Task IfElseWithNonCountCondition_ShouldNotReport()
        {
            var source = @"
class TestClass
{
    void TestMethod()
    {
        bool flag = true;
        string text;
        if (flag) { text = ""hello""; } else { text = ""world""; }
    }
}";
            var compilation = CreateCompilation(source);
            var analyzer = new EnglishOnlyPluralizationAnalyzer();
            var compWithAnalyzers = compilation.WithAnalyzers(new[] { (DiagnosticAnalyzer)analyzer }.ToImmutableArray());
            var diagnostics = await compWithAnalyzers.GetAnalyzerDiagnosticsAsync();
            var loc014s = diagnostics.Where(d => d.Id == "LOC014").ToList();
            Assert.AreEqual(0, loc014s.Count, "Should not report LOC014 for non-count condition");
        }
    }
}
