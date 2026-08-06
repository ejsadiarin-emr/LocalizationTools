using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LocalizationAnalyzers.Tests
{
    [TestClass]
    public class HardcodedDateFormatAnalyzerTests
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
        public async Task DateTimeToStringWithDateFormat_ShouldReportLOC005()
        {
            var source = @"
using System;
class TestClass
{
    void TestMethod()
    {
        DateTime now = DateTime.Now;
        string formatted = now.ToString(""dd/MM/yyyy"");
    }
}";
            var compilation = CreateCompilation(source);
            var analyzer = new Analyzers.HardcodedDateFormatAnalyzer();
            var compWithAnalyzers = compilation.WithAnalyzers(new[] { (Microsoft.CodeAnalysis.Diagnostics.DiagnosticAnalyzer)analyzer }.ToImmutableArray());
            var diagnostics = await compWithAnalyzers.GetAnalyzerDiagnosticsAsync();
            var loc005s = diagnostics.Where(d => d.Id == "LOC005").ToList();
            Assert.AreEqual(1, loc005s.Count);
        }

        [TestMethod]
        public async Task DateTimeToStringWithTimeFormat_ShouldReportLOC005()
        {
            var source = @"
using System;
class TestClass
{
    void TestMethod()
    {
        DateTime now = DateTime.Now;
        string formatted = now.ToString(""hh:mm tt"");
    }
}";
            var compilation = CreateCompilation(source);
            var analyzer = new Analyzers.HardcodedDateFormatAnalyzer();
            var compWithAnalyzers = compilation.WithAnalyzers(new[] { (Microsoft.CodeAnalysis.Diagnostics.DiagnosticAnalyzer)analyzer }.ToImmutableArray());
            var diagnostics = await compWithAnalyzers.GetAnalyzerDiagnosticsAsync();
            var loc005s = diagnostics.Where(d => d.Id == "LOC005").ToList();
            Assert.AreEqual(1, loc005s.Count);
        }

        [TestMethod]
        public async Task DoubleToStringWithNumberFormat_ShouldReportLOC005()
        {
            var source = @"
using System;
class TestClass
{
    void TestMethod()
    {
        double price = 1234.56;
        string formatted = price.ToString(""#,##0.00"");
    }
}";
            var compilation = CreateCompilation(source);
            var analyzer = new Analyzers.HardcodedDateFormatAnalyzer();
            var compWithAnalyzers = compilation.WithAnalyzers(new[] { (Microsoft.CodeAnalysis.Diagnostics.DiagnosticAnalyzer)analyzer }.ToImmutableArray());
            var diagnostics = await compWithAnalyzers.GetAnalyzerDiagnosticsAsync();
            var loc005s = diagnostics.Where(d => d.Id == "LOC005").ToList();
            Assert.AreEqual(1, loc005s.Count);
        }

        [TestMethod]
        public async Task DoubleToStringWithCurrencyFormat_ShouldReportLOC005()
        {
            var source = @"
using System;
class TestClass
{
    void TestMethod()
    {
        double price = 1234.56;
        string formatted = price.ToString(""C2"");
    }
}";
            var compilation = CreateCompilation(source);
            var analyzer = new Analyzers.HardcodedDateFormatAnalyzer();
            var compWithAnalyzers = compilation.WithAnalyzers(new[] { (Microsoft.CodeAnalysis.Diagnostics.DiagnosticAnalyzer)analyzer }.ToImmutableArray());
            var diagnostics = await compWithAnalyzers.GetAnalyzerDiagnosticsAsync();
            var loc005s = diagnostics.Where(d => d.Id == "LOC005").ToList();
            Assert.AreEqual(1, loc005s.Count);
        }

        [TestMethod]
        public async Task ToStringWithoutFormat_ShouldNotReport()
        {
            var source = @"
using System;
class TestClass
{
    void TestMethod()
    {
        DateTime now = DateTime.Now;
        string formatted = now.ToString();
    }
}";
            var compilation = CreateCompilation(source);
            var analyzer = new Analyzers.HardcodedDateFormatAnalyzer();
            var compWithAnalyzers = compilation.WithAnalyzers(new[] { (Microsoft.CodeAnalysis.Diagnostics.DiagnosticAnalyzer)analyzer }.ToImmutableArray());
            var diagnostics = await compWithAnalyzers.GetAnalyzerDiagnosticsAsync();
            var loc005s = diagnostics.Where(d => d.Id == "LOC005").ToList();
            Assert.AreEqual(0, loc005s.Count);
        }

        [TestMethod]
        public async Task ToStringWithCultureInfo_ShouldNotReport()
        {
            var source = @"
using System;
using System.Globalization;
class TestClass
{
    void TestMethod()
    {
        DateTime now = DateTime.Now;
        string formatted = now.ToString(""d"", CultureInfo.CurrentCulture);
    }
}";
            var compilation = CreateCompilation(source);
            var analyzer = new Analyzers.HardcodedDateFormatAnalyzer();
            var compWithAnalyzers = compilation.WithAnalyzers(new[] { (Microsoft.CodeAnalysis.Diagnostics.DiagnosticAnalyzer)analyzer }.ToImmutableArray());
            var diagnostics = await compWithAnalyzers.GetAnalyzerDiagnosticsAsync();
            var loc005s = diagnostics.Where(d => d.Id == "LOC005").ToList();
            Assert.AreEqual(0, loc005s.Count);
        }

        [TestMethod]
        public async Task IntegerToString_ShouldNotReport()
        {
            var source = @"
class TestClass
{
    void TestMethod()
    {
        int count = 42;
        string formatted = count.ToString(""D4"");
    }
}";
            var compilation = CreateCompilation(source);
            var analyzer = new Analyzers.HardcodedDateFormatAnalyzer();
            var compWithAnalyzers = compilation.WithAnalyzers(new[] { (Microsoft.CodeAnalysis.Diagnostics.DiagnosticAnalyzer)analyzer }.ToImmutableArray());
            var diagnostics = await compWithAnalyzers.GetAnalyzerDiagnosticsAsync();
            var loc005s = diagnostics.Where(d => d.Id == "LOC005").ToList();
            Assert.AreEqual(0, loc005s.Count);
        }
    }
}
