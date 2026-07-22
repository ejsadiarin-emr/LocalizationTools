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
    public class PunctuationOutsideStringAnalyzerTests
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
        public async Task PunctuationConcatenatedAfterString_ShouldReportLOC015()
        {
            var source = @"
class TestClass
{
    void TestMethod()
    {
        var label = new Label();
        label.Text = ""label"" + "":"";
    }
}
class Label { public string Text { get; set; } }";
            var compilation = CreateCompilation(source);
            var analyzer = new PunctuationOutsideStringAnalyzer();
            var compWithAnalyzers = compilation.WithAnalyzers(new[] { (DiagnosticAnalyzer)analyzer }.ToImmutableArray());
            var diagnostics = await compWithAnalyzers.GetAnalyzerDiagnosticsAsync();
            var loc015s = diagnostics.Where(d => d.Id == "LOC015").ToList();
            Assert.AreEqual(1, loc015s.Count, "Expected LOC015 for punctuation concatenated after string");
        }

        [TestMethod]
        public async Task PunctuationInOutputContext_ShouldReportLOC015()
        {
            var source = @"
class TestClass
{
    void TestMethod()
    {
        System.Console.WriteLine(""label"" + ""."");
    }
}";
            var compilation = CreateCompilation(source);
            var analyzer = new PunctuationOutsideStringAnalyzer();
            var compWithAnalyzers = compilation.WithAnalyzers(new[] { (DiagnosticAnalyzer)analyzer }.ToImmutableArray());
            var diagnostics = await compWithAnalyzers.GetAnalyzerDiagnosticsAsync();
            var loc015s = diagnostics.Where(d => d.Id == "LOC015").ToList();
            Assert.AreEqual(1, loc015s.Count, "Expected LOC015 for punctuation in output context");
        }

        [TestMethod]
        public async Task PunctuationInsideString_ShouldNotReport()
        {
            var source = @"
class TestClass
{
    void TestMethod()
    {
        var label = new Label();
        label.Text = ""label:"";
    }
}
class Label { public string Text { get; set; } }";
            var compilation = CreateCompilation(source);
            var analyzer = new PunctuationOutsideStringAnalyzer();
            var compWithAnalyzers = compilation.WithAnalyzers(new[] { (DiagnosticAnalyzer)analyzer }.ToImmutableArray());
            var diagnostics = await compWithAnalyzers.GetAnalyzerDiagnosticsAsync();
            var loc015s = diagnostics.Where(d => d.Id == "LOC015").ToList();
            Assert.AreEqual(0, loc015s.Count, "Should not report LOC015 for punctuation inside string");
        }

        [TestMethod]
        public async Task NonUserFacingContext_ShouldNotReport()
        {
            var source = @"
class TestClass
{
    void TestMethod()
    {
        string path = ""dir"" + ""/"" + ""file.txt"";
    }
}";
            var compilation = CreateCompilation(source);
            var analyzer = new PunctuationOutsideStringAnalyzer();
            var compWithAnalyzers = compilation.WithAnalyzers(new[] { (DiagnosticAnalyzer)analyzer }.ToImmutableArray());
            var diagnostics = await compWithAnalyzers.GetAnalyzerDiagnosticsAsync();
            var loc015s = diagnostics.Where(d => d.Id == "LOC015").ToList();
            Assert.AreEqual(0, loc015s.Count, "Should not report LOC015 for non-user-facing context");
        }
    }
}
