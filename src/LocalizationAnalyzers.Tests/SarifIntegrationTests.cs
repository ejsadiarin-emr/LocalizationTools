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

        [TestMethod]
        public async Task SarifOutput_AllNewRulesRegistered()
        {
            Microsoft.CodeAnalysis.Diagnostics.DiagnosticAnalyzer[] analyzers = new Microsoft.CodeAnalysis.Diagnostics.DiagnosticAnalyzer[]
            {
                new Analyzers.StringInterpolationAnalyzer(),
                new Analyzers.HardcodedDateTimeFormatAnalyzer(),
                new Analyzers.DynamicResourceKeyAnalyzer(),
                new Analyzers.EnglishOnlyPluralizationAnalyzer(),
                new Analyzers.PunctuationOutsideStringAnalyzer()
            };

            var supportedDiagnostics = analyzers.SelectMany(a => a.SupportedDiagnostics).Select(d => d.Id).ToList();
            CollectionAssert.Contains(supportedDiagnostics, "LOC011");
            CollectionAssert.Contains(supportedDiagnostics, "LOC012");
            CollectionAssert.Contains(supportedDiagnostics, "LOC013");
            CollectionAssert.Contains(supportedDiagnostics, "LOC014");
            CollectionAssert.Contains(supportedDiagnostics, "LOC015");
        }

        [TestMethod]
        public async Task SarifOutput_LOC011StringInterpolationInLocalizer()
        {
            var source = @"
class TestClass {
    void DoWork() {
        var localizer = new FakeLocalizer();
        string name = ""world"";
        var x = localizer[$""Hello {name}""];
    }
}
class FakeLocalizer { public string this[string k] => k; }";
            var compilation = CreateCompilation(source);
            var analyzer = new Analyzers.StringInterpolationAnalyzer();
            var compWithAnalyzers = compilation.WithAnalyzers(new[] { (DiagnosticAnalyzer)analyzer }.ToImmutableArray());
            var diagnostics = await compWithAnalyzers.GetAnalyzerDiagnosticsAsync();

            var loc011s = diagnostics.Where(d => d.Id == "LOC011").ToList();
            Assert.IsTrue(loc011s.Any(), "Should detect LOC011");
            Assert.AreEqual(DiagnosticSeverity.Warning, loc011s[0].Severity);
        }

        [TestMethod]
        public async Task SarifOutput_LOC012HardcodedDateTimeFormat()
        {
            var source = @"
class TestClass {
    void DoWork() {
        var dt = new System.DateTime(2024, 1, 1);
        var s = dt.ToString(""MM/dd/yyyy"");
    }
}";
            var compilation = CreateCompilation(source);
            var analyzer = new Analyzers.HardcodedDateTimeFormatAnalyzer();
            var compWithAnalyzers = compilation.WithAnalyzers(new[] { (DiagnosticAnalyzer)analyzer }.ToImmutableArray());
            var diagnostics = await compWithAnalyzers.GetAnalyzerDiagnosticsAsync();

            var loc012s = diagnostics.Where(d => d.Id == "LOC012").ToList();
            Assert.IsTrue(loc012s.Any(), "Should detect LOC012");
            Assert.AreEqual(DiagnosticSeverity.Warning, loc012s[0].Severity);
        }

        [TestMethod]
        public async Task SarifOutput_LOC013DynamicResourceKey()
        {
            var source = @"
class TestClass {
    void DoWork() {
        var localizer = new FakeLocalizer();
        string code = ""404"";
        var x = localizer[""Error_"" + code];
    }
}
class FakeLocalizer { public string this[string k] => k; }";
            var compilation = CreateCompilation(source);
            var analyzer = new Analyzers.DynamicResourceKeyAnalyzer();
            var compWithAnalyzers = compilation.WithAnalyzers(new[] { (DiagnosticAnalyzer)analyzer }.ToImmutableArray());
            var diagnostics = await compWithAnalyzers.GetAnalyzerDiagnosticsAsync();

            var loc013s = diagnostics.Where(d => d.Id == "LOC013").ToList();
            Assert.IsTrue(loc013s.Any(), "Should detect LOC013");
            Assert.AreEqual(DiagnosticSeverity.Info, loc013s[0].Severity);
        }

        [TestMethod]
        public async Task SarifOutput_LOC014IfElsePluralization()
        {
            var source = @"
class TestClass {
    void DoWork() {
        int count = 5;
        string text;
        if (count == 1) { text = ""item""; } else { text = ""items""; }
    }
}";
            var compilation = CreateCompilation(source);
            var analyzer = new Analyzers.EnglishOnlyPluralizationAnalyzer();
            var compWithAnalyzers = compilation.WithAnalyzers(new[] { (DiagnosticAnalyzer)analyzer }.ToImmutableArray());
            var diagnostics = await compWithAnalyzers.GetAnalyzerDiagnosticsAsync();

            var loc014s = diagnostics.Where(d => d.Id == "LOC014").ToList();
            Assert.IsTrue(loc014s.Any(), "Should detect LOC014");
            Assert.AreEqual(DiagnosticSeverity.Warning, loc014s[0].Severity);
        }

        [TestMethod]
        public async Task SarifOutput_LOC015PunctuationOutsideString()
        {
            var source = @"
class Label { public string Text { get; set; } }
class TestClass {
    void DoWork() {
        var label = new Label();
        label.Text = ""label"" + "":"";
    }
}";
            var compilation = CreateCompilation(source);
            var analyzer = new Analyzers.PunctuationOutsideStringAnalyzer();
            var compWithAnalyzers = compilation.WithAnalyzers(new[] { (DiagnosticAnalyzer)analyzer }.ToImmutableArray());
            var diagnostics = await compWithAnalyzers.GetAnalyzerDiagnosticsAsync();

            var loc015s = diagnostics.Where(d => d.Id == "LOC015").ToList();
            Assert.IsTrue(loc015s.Any(), "Should detect LOC015");
            Assert.AreEqual(DiagnosticSeverity.Info, loc015s[0].Severity);
        }

        [TestMethod]
        public void CaAnalyzers_CanBeDiscoveredFromNetAnalyzersAssembly()
        {
            var netAnalyzersAssembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name?.StartsWith("Microsoft.CodeAnalysis.NetAnalyzers") == true);

            if (netAnalyzersAssembly == null)
            {
                Assert.Inconclusive("Microsoft.CodeAnalysis.NetAnalyzers assembly not loaded");
                return;
            }

            var analyzerTypes = netAnalyzersAssembly.GetTypes()
                .Where(t => typeof(DiagnosticAnalyzer).IsAssignableFrom(t) && !t.IsAbstract)
                .ToList();

            Assert.IsTrue(analyzerTypes.Count > 0, "Should discover CA analyzer types from NetAnalyzers assembly");
        }

        [TestMethod]
        public void CaAnalyzers_IncludeGlobalizationRules()
        {
            var netAnalyzersAssembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name?.StartsWith("Microsoft.CodeAnalysis.NetAnalyzers") == true);

            if (netAnalyzersAssembly == null)
            {
                Assert.Inconclusive("Microsoft.CodeAnalysis.NetAnalyzers assembly not loaded");
                return;
            }

            var analyzers = netAnalyzersAssembly.GetTypes()
                .Where(t => typeof(DiagnosticAnalyzer).IsAssignableFrom(t) && !t.IsAbstract)
                .Select(t => (DiagnosticAnalyzer)Activator.CreateInstance(t)!)
                .ToList();

            var allDiagnosticIds = analyzers
                .SelectMany(a => a.SupportedDiagnostics)
                .Select(d => d.Id)
                .ToList();

            var expectedCaRules = new[] { "CA1303", "CA1304", "CA1305", "CA1307", "CA1308", "CA1309", "CA1310", "CA1311" };
            foreach (var ruleId in expectedCaRules)
            {
                CollectionAssert.Contains(allDiagnosticIds, ruleId, $"CA rule {ruleId} should be discoverable");
            }
        }

        [TestMethod]
        public void AllNewRules_HaveCorrectSeverities()
        {
            Microsoft.CodeAnalysis.Diagnostics.DiagnosticAnalyzer[] analyzers = new Microsoft.CodeAnalysis.Diagnostics.DiagnosticAnalyzer[]
            {
                new Analyzers.StringInterpolationAnalyzer(),
                new Analyzers.HardcodedDateTimeFormatAnalyzer(),
                new Analyzers.DynamicResourceKeyAnalyzer(),
                new Analyzers.EnglishOnlyPluralizationAnalyzer(),
                new Analyzers.PunctuationOutsideStringAnalyzer()
            };

            var allDiagnostics = analyzers.SelectMany(a => a.SupportedDiagnostics).ToDictionary(d => d.Id, d => d);

            Assert.AreEqual(DiagnosticSeverity.Warning, allDiagnostics["LOC011"].DefaultSeverity);
            Assert.AreEqual(DiagnosticSeverity.Warning, allDiagnostics["LOC012"].DefaultSeverity);
            Assert.AreEqual(DiagnosticSeverity.Info, allDiagnostics["LOC013"].DefaultSeverity);
            Assert.AreEqual(DiagnosticSeverity.Warning, allDiagnostics["LOC014"].DefaultSeverity);
            Assert.AreEqual(DiagnosticSeverity.Info, allDiagnostics["LOC015"].DefaultSeverity);
        }

        [TestMethod]
        public void AllRules_CoverAllLocIds()
        {
            Microsoft.CodeAnalysis.Diagnostics.DiagnosticAnalyzer[] locAnalyzers = new Microsoft.CodeAnalysis.Diagnostics.DiagnosticAnalyzer[]
            {
                new Analyzers.StringInConditionalAnalyzer(),
                new Analyzers.StringInDataAccessAnalyzer(),
                new Analyzers.StringInEqualityAnalyzer(),
                new Analyzers.DisplayStringAnalyzer(),
                new Analyzers.StringConcatenationAnalyzer(),
                new Analyzers.HardcodedDateFormatAnalyzer(),
                new Analyzers.MissingStringComparisonAnalyzer(),
                new Analyzers.HardcodedPluralLogicAnalyzer(),
                new Analyzers.StringInterpolationAnalyzer(),
                new Analyzers.HardcodedDateTimeFormatAnalyzer(),
                new Analyzers.DynamicResourceKeyAnalyzer(),
                new Analyzers.EnglishOnlyPluralizationAnalyzer(),
                new Analyzers.PunctuationOutsideStringAnalyzer()
            };

            var allIds = locAnalyzers.SelectMany(a => a.SupportedDiagnostics).Select(d => d.Id).Distinct().ToList();
            var expectedIds = new[] { "LOC001", "LOC002", "LOC003", "LOC004", "LOC005", "LOC006", "LOC007", "LOC010", "LOC011", "LOC012", "LOC013", "LOC014", "LOC015" };

            foreach (var id in expectedIds)
            {
                CollectionAssert.Contains(allIds, id, $"Rule {id} should be registered");
            }
        }
    }
}