using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LocalizationAnalyzers.Tests
{
    [TestClass]
    public class SarifIntegrationTests
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
        public async Task SarifOutput_ContainsLOC001()
        {
            var source = @"
class TestClass {
    void DoWork(string input) {
        if (input == ""test"") { }
    }
}";
            var compilation = CreateCompilation(source);
            var analyzer = new Analyzers.StringInConditionalAnalyzer();
            var compWithAnalyzers = compilation.WithAnalyzers(new[] { (Microsoft.CodeAnalysis.Diagnostics.DiagnosticAnalyzer)analyzer }.ToImmutableArray());
            var diagnostics = await compWithAnalyzers.GetAnalyzerDiagnosticsAsync();

            var dv001s = diagnostics.Where(d => d.Id == "LOC001").ToList();
            Assert.IsTrue(dv001s.Any(), "Should detect LOC001");
            Assert.AreEqual(DiagnosticSeverity.Warning, dv001s[0].Severity);
        }

        [TestMethod]
        public async Task SarifOutput_ContainsLOC010()
        {
            var source = @"
class Label { public string Text { get; set; } }
class TestClass {
    void DoWork() {
        var label = new Label();
        label.Text = ""Hello World"";
    }
}";
            var compilation = CreateCompilation(source);
            var analyzer = new Analyzers.DisplayStringAnalyzer();
            var compWithAnalyzers = compilation.WithAnalyzers(new[] { (Microsoft.CodeAnalysis.Diagnostics.DiagnosticAnalyzer)analyzer }.ToImmutableArray());
            var diagnostics = await compWithAnalyzers.GetAnalyzerDiagnosticsAsync();

            var dv010s = diagnostics.Where(d => d.Id == "LOC010").ToList();
            Assert.IsTrue(dv010s.Any(), "Should detect LOC010");
        }

        [TestMethod]
        public void SarifOutput_AllFourRulesRegistered()
        {
            Microsoft.CodeAnalysis.Diagnostics.DiagnosticAnalyzer[] analyzers = new Microsoft.CodeAnalysis.Diagnostics.DiagnosticAnalyzer[]
            {
                new Analyzers.StringInConditionalAnalyzer(),
                new Analyzers.StringInDataAccessAnalyzer(),
                new Analyzers.StringInEqualityAnalyzer(),
                new Analyzers.DisplayStringAnalyzer()
            };

            var supportedDiagnostics = analyzers.SelectMany(a => a.SupportedDiagnostics).Select(d => d.Id).ToList();
            CollectionAssert.Contains(supportedDiagnostics, "LOC001");
            CollectionAssert.Contains(supportedDiagnostics, "LOC002");
            CollectionAssert.Contains(supportedDiagnostics, "LOC003");
            CollectionAssert.Contains(supportedDiagnostics, "LOC010");
        }

        [TestMethod]
        public async Task SarifOutput_DiagnosticsHaveCorrectSeverity()
        {
            var source = @"
class TestClass {
    void DoWork(string input) {
        if (input == ""test"") { }
    }
}";
            var compilation = CreateCompilation(source);
            var analyzer = new Analyzers.StringInConditionalAnalyzer();
            var compWithAnalyzers = compilation.WithAnalyzers(new[] { (Microsoft.CodeAnalysis.Diagnostics.DiagnosticAnalyzer)analyzer }.ToImmutableArray());
            var diagnostics = await compWithAnalyzers.GetAnalyzerDiagnosticsAsync();

            var diagnostic = diagnostics.First(d => d.Id == "LOC001");
            Assert.AreEqual(DiagnosticSeverity.Warning, diagnostic.Severity);
            Assert.IsTrue(diagnostic.GetMessage().Contains("test"));
        }

        [TestMethod]
        public async Task SarifOutput_DiagnosticsHaveLocation()
        {
            var source = @"
class TestClass {
    void DoWork(string input) {
        if (input == ""test"") { }
    }
}";
            var compilation = CreateCompilation(source);
            var analyzer = new Analyzers.StringInConditionalAnalyzer();
            var compWithAnalyzers = compilation.WithAnalyzers(new[] { (Microsoft.CodeAnalysis.Diagnostics.DiagnosticAnalyzer)analyzer }.ToImmutableArray());
            var diagnostics = await compWithAnalyzers.GetAnalyzerDiagnosticsAsync();

            var diagnostic = diagnostics.First(d => d.Id == "LOC001");
            var location = diagnostic.Location;
            Assert.IsNotNull(location);
            Assert.IsTrue(location.IsInSource);
            var span = location.GetLineSpan();
            Assert.AreEqual(3, span.StartLinePosition.Line); // Line 4 (0-indexed = 3)
        }

        [TestMethod]
        public async Task SarifOutput_DiagnosticsHaveAllRequiredFields()
        {
            var source = @"
class TestClass {
    void DoWork(string input) {
        if (input == ""test"") { }
    }
}";
            var compilation = CreateCompilation(source);
            var analyzer = new Analyzers.StringInConditionalAnalyzer();
            var compWithAnalyzers = compilation.WithAnalyzers(new[] { (Microsoft.CodeAnalysis.Diagnostics.DiagnosticAnalyzer)analyzer }.ToImmutableArray());
            var diagnostics = await compWithAnalyzers.GetAnalyzerDiagnosticsAsync();

            var diagnostic = diagnostics.First(d => d.Id == "LOC001");
            
            // Verify all required SARIF fields are present
            Assert.IsNotNull(diagnostic.Id, "Diagnostic must have an ID");
            Assert.IsNotNull(diagnostic.GetMessage(), "Diagnostic must have a message");
            Assert.IsNotNull(diagnostic.Severity, "Diagnostic must have a severity");
            Assert.IsNotNull(diagnostic.Location, "Diagnostic must have a location");
            Assert.IsTrue(diagnostic.Location.IsInSource, "Diagnostic location must be in source");
            
            // Verify SARIF-compatible location data
            var span = diagnostic.Location.GetLineSpan();
            Assert.IsTrue(span.StartLinePosition.Line >= 0, "Line number must be non-negative");
            Assert.IsTrue(span.StartLinePosition.Character >= 0, "Column number must be non-negative");
        }

        [TestMethod]
        public async Task SarifOutput_LOC002AndLOC003HaveCorrectSeverity()
        {
            var source = @"
class TestClass {
    void DoWork() {
        var db = new Database();
        var result = db.Find(""test"");
        
        string status = ""running"";
        bool match = status == ""test"";
    }
}

class Database {
    public object Find(string query) { return null; }
}";
            var compilation = CreateCompilation(source);
            var analyzers = new Microsoft.CodeAnalysis.Diagnostics.DiagnosticAnalyzer[]
            {
                new Analyzers.StringInDataAccessAnalyzer(),
                new Analyzers.StringInEqualityAnalyzer()
            };
            var compWithAnalyzers = compilation.WithAnalyzers(analyzers.ToImmutableArray());
            var diagnostics = await compWithAnalyzers.GetAnalyzerDiagnosticsAsync();

            var dv002s = diagnostics.Where(d => d.Id == "LOC002").ToList();
            var dv003s = diagnostics.Where(d => d.Id == "LOC003").ToList();

            Assert.IsTrue(dv002s.Any(), "Should detect LOC002");
            Assert.IsTrue(dv003s.Any(), "Should detect LOC003");
            
            // Both LOC002 and LOC003 should be Warning severity
            Assert.AreEqual(DiagnosticSeverity.Warning, dv002s[0].Severity);
            Assert.AreEqual(DiagnosticSeverity.Warning, dv003s[0].Severity);
        }

        [TestMethod]
        public async Task SarifOutput_LOC010IsInfoSeverity()
        {
            var source = @"
class Label { public string Text { get; set; } }
class TestClass {
    void DoWork() {
        var label = new Label();
        label.Text = ""Hello World"";
    }
}";
            var compilation = CreateCompilation(source);
            var analyzer = new Analyzers.DisplayStringAnalyzer();
            var compWithAnalyzers = compilation.WithAnalyzers(new[] { (Microsoft.CodeAnalysis.Diagnostics.DiagnosticAnalyzer)analyzer }.ToImmutableArray());
            var diagnostics = await compWithAnalyzers.GetAnalyzerDiagnosticsAsync();

            var dv010s = diagnostics.Where(d => d.Id == "LOC010").ToList();
            Assert.IsTrue(dv010s.Any(), "Should detect LOC010");
            
            // LOC010 should be Info severity (not Warning)
            Assert.AreEqual(DiagnosticSeverity.Info, dv010s[0].Severity);
        }
    }
}