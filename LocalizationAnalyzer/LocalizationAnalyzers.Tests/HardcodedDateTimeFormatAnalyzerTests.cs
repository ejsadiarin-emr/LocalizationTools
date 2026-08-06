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
    public class HardcodedDateTimeFormatAnalyzerTests
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
        public async Task HardcodedDateFormatInToString_ShouldReportLOC012()
        {
            var source = @"
class TestClass
{
    void TestMethod()
    {
        var dt = new System.DateTime(2024, 1, 1);
        var s = dt.ToString(""MM/dd/yyyy"");
    }
}";
            var compilation = CreateCompilation(source);
            var analyzer = new HardcodedDateTimeFormatAnalyzer();
            var compWithAnalyzers = compilation.WithAnalyzers(new[] { (DiagnosticAnalyzer)analyzer }.ToImmutableArray());
            var diagnostics = await compWithAnalyzers.GetAnalyzerDiagnosticsAsync();
            var loc012s = diagnostics.Where(d => d.Id == "LOC012").ToList();
            Assert.AreEqual(1, loc012s.Count, "Expected LOC012 for hardcoded date format in ToString");
        }

        [TestMethod]
        public async Task HardcodedTimeFormatInToString_ShouldReportLOC012()
        {
            var source = @"
class TestClass
{
    void TestMethod()
    {
        var dt = new System.DateTime(2024, 1, 1);
        var s = dt.ToString(""hh:mm:ss"");
    }
}";
            var compilation = CreateCompilation(source);
            var analyzer = new HardcodedDateTimeFormatAnalyzer();
            var compWithAnalyzers = compilation.WithAnalyzers(new[] { (DiagnosticAnalyzer)analyzer }.ToImmutableArray());
            var diagnostics = await compWithAnalyzers.GetAnalyzerDiagnosticsAsync();
            var loc012s = diagnostics.Where(d => d.Id == "LOC012").ToList();
            Assert.AreEqual(1, loc012s.Count, "Expected LOC012 for hardcoded time format in ToString");
        }

        [TestMethod]
        public async Task FormatWithCultureInfo_ShouldNotReport()
        {
            var source = @"
class TestClass
{
    void TestMethod()
    {
        var dt = new System.DateTime(2024, 1, 1);
        var s = dt.ToString(""MM/dd/yyyy"", System.Globalization.CultureInfo.InvariantCulture);
    }
}";
            var compilation = CreateCompilation(source);
            var analyzer = new HardcodedDateTimeFormatAnalyzer();
            var compWithAnalyzers = compilation.WithAnalyzers(new[] { (DiagnosticAnalyzer)analyzer }.ToImmutableArray());
            var diagnostics = await compWithAnalyzers.GetAnalyzerDiagnosticsAsync();
            var loc012s = diagnostics.Where(d => d.Id == "LOC012").ToList();
            Assert.AreEqual(0, loc012s.Count, "Should not report LOC012 when CultureInfo is provided");
        }

        [TestMethod]
        public async Task StandardFormatSpecifier_ShouldNotReport()
        {
            var source = @"
class TestClass
{
    void TestMethod()
    {
        var dt = new System.DateTime(2024, 1, 1);
        var s = dt.ToString(""D"");
    }
}";
            var compilation = CreateCompilation(source);
            var analyzer = new HardcodedDateTimeFormatAnalyzer();
            var compWithAnalyzers = compilation.WithAnalyzers(new[] { (DiagnosticAnalyzer)analyzer }.ToImmutableArray());
            var diagnostics = await compWithAnalyzers.GetAnalyzerDiagnosticsAsync();
            var loc012s = diagnostics.Where(d => d.Id == "LOC012").ToList();
            Assert.AreEqual(0, loc012s.Count, "Should not report LOC012 for standard format specifier");
        }
    }
}
