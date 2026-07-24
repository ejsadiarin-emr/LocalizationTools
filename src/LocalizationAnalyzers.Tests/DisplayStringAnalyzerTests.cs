using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LocalizationAnalyzers.Tests
{
    [TestClass]
    public class DisplayStringAnalyzerTests
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

        private static async Task<List<Diagnostic>> GetDiagnostics(string source, Analyzers.DisplayStringAnalyzer analyzer, string? fileName = null)
        {
            var syntaxTree = string.IsNullOrEmpty(fileName)
                ? CSharpSyntaxTree.ParseText(source)
                : CSharpSyntaxTree.ParseText(source, path: fileName);
            var compilation = CSharpCompilation.Create(
                "TestAssembly",
                new[] { syntaxTree },
                new[] { CorlibReference, ConsoleReference },
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            var compWithAnalyzers = compilation.WithAnalyzers(new[] { (Microsoft.CodeAnalysis.Diagnostics.DiagnosticAnalyzer)analyzer }.ToImmutableArray());
            return (await compWithAnalyzers.GetAnalyzerDiagnosticsAsync()).ToList();
        }

        [TestMethod]
        public async Task StringAssignedToTextProperty_ShouldReportLOC010()
        {
            var source = @"
class Label { public string Text { get; set; } }
class TestClass
{
    void TestMethod()
    {
        var label = new Label();
        label.Text = ""Hello"";
    }
}";
            var diagnostics = await GetDiagnostics(source, new Analyzers.DisplayStringAnalyzer());
            var dv010s = diagnostics.Where(d => d.Id == "LOC010").ToList();
            Assert.AreEqual(1, dv010s.Count);
            Assert.IsTrue(dv010s[0].GetMessage().Contains("Hello"));
        }

        [TestMethod]
        public async Task StringInButtonConstructor_ShouldReportLOC010()
        {
            var source = @"
class Button { public Button(string text) {} }
class TestClass
{
    void TestMethod()
    {
        var button = new Button(""Click Me"");
    }
}";
            var diagnostics = await GetDiagnostics(source, new Analyzers.DisplayStringAnalyzer());
            var dv010s = diagnostics.Where(d => d.Id == "LOC010").ToList();
            Assert.AreEqual(1, dv010s.Count);
        }

        [TestMethod]
        public async Task StringInConsoleWriteLine_ShouldReportLOC010()
        {
            var source = @"
using System;
class TestClass
{
    void TestMethod()
    {
        Console.WriteLine(""Hello World"");
    }
}";
            var diagnostics = await GetDiagnostics(source, new Analyzers.DisplayStringAnalyzer());
            var dv010s = diagnostics.Where(d => d.Id == "LOC010").ToList();
            Assert.AreEqual(1, dv010s.Count);
        }

        [TestMethod]
        public async Task NonUiProperty_ShouldNotReport()
        {
            var source = @"
class TestClass
{
    public string Name { get; set; }
    void TestMethod()
    {
        var obj = new TestClass();
        obj.Name = ""test"";
    }
}";
            var diagnostics = await GetDiagnostics(source, new Analyzers.DisplayStringAnalyzer());
            var dv010s = diagnostics.Where(d => d.Id == "LOC010").ToList();
            Assert.AreEqual(0, dv010s.Count);
        }

        [TestMethod]
        public async Task StringInResourceReference_ShouldNotReport()
        {
            var source = @"
class Strings { public static string Hello = ""hello""; }
class Label { public string Text { get; set; } }
class TestClass
{
    void TestMethod()
    {
        var label = new Label();
        label.Text = Strings.Hello;
    }
}";
            var diagnostics = await GetDiagnostics(source, new Analyzers.DisplayStringAnalyzer());
            var dv010s = diagnostics.Where(d => d.Id == "LOC010").ToList();
            Assert.AreEqual(0, dv010s.Count);
        }

        [TestMethod]
        public async Task DebugWriteLine_ShouldNotReportLOC010()
        {
            var source = @"
using System.Diagnostics;
class TestClass
{
    void TestMethod()
    {
        Debug.WriteLine(""Hello World"");
    }
}";
            var diagnostics = await GetDiagnostics(source, new Analyzers.DisplayStringAnalyzer());
            var dv010s = diagnostics.Where(d => d.Id == "LOC010").ToList();
            Assert.AreEqual(0, dv010s.Count);
        }

        [TestMethod]
        public async Task TraceWriteLine_ShouldNotReportLOC010()
        {
            var source = @"
using System.Diagnostics;
class TestClass
{
    void TestMethod()
    {
        Trace.WriteLine(""Hello World"");
    }
}";
            var diagnostics = await GetDiagnostics(source, new Analyzers.DisplayStringAnalyzer());
            var dv010s = diagnostics.Where(d => d.Id == "LOC010").ToList();
            Assert.AreEqual(0, dv010s.Count);
        }

        [TestMethod]
        public async Task LoggerLogDebug_ShouldReportLOC010()
        {
            var source = @"
interface ILogger { void LogDebug(string message); }
class TestClass
{
    void TestMethod(ILogger logger)
    {
        logger.LogDebug(""Hello World"");
    }
}";
            var diagnostics = await GetDiagnostics(source, new Analyzers.DisplayStringAnalyzer());
            var dv010s = diagnostics.Where(d => d.Id == "LOC010").ToList();
            Assert.AreEqual(1, dv010s.Count);
        }

        [TestMethod]
        public async Task CustomLoggerClass_ShouldReportLOC010()
        {
            var source = @"
class MyAppLogger { public void Log(string message) {} }
class TestClass
{
    void TestMethod()
    {
        var logger = new MyAppLogger();
        logger.Log(""Hello World"");
    }
}";
            var diagnostics = await GetDiagnostics(source, new Analyzers.DisplayStringAnalyzer());
            var dv010s = diagnostics.Where(d => d.Id == "LOC010").ToList();
            Assert.AreEqual(1, dv010s.Count);
        }

        [TestMethod]
        public async Task ButtonText_ShouldReportLOC010()
        {
            var source = @"
class Button { public string Text { get; set; } }
class TestClass
{
    void TestMethod()
    {
        var button = new Button();
        button.Text = ""OK"";
    }
}";
            var diagnostics = await GetDiagnostics(source, new Analyzers.DisplayStringAnalyzer());
            var dv010s = diagnostics.Where(d => d.Id == "LOC010").ToList();
            Assert.AreEqual(1, dv010s.Count);
        }

        [TestMethod]
        public async Task ConfigText_ShouldNotReportLOC010()
        {
            var source = @"
class Config { public string Text { get; set; } }
class TestClass
{
    void TestMethod()
    {
        var config = new Config();
        config.Text = ""setting"";
    }
}";
            var diagnostics = await GetDiagnostics(source, new Analyzers.DisplayStringAnalyzer());
            var dv010s = diagnostics.Where(d => d.Id == "LOC010").ToList();
            Assert.AreEqual(0, dv010s.Count);
        }

        [TestMethod]
        public async Task DialogTitle_ShouldReportLOC010()
        {
            var source = @"
class Dialog { public string Title { get; set; } }
class TestClass
{
    void TestMethod()
    {
        var dialog = new Dialog();
        dialog.Title = ""Settings"";
    }
}";
            var diagnostics = await GetDiagnostics(source, new Analyzers.DisplayStringAnalyzer());
            var dv010s = diagnostics.Where(d => d.Id == "LOC010").ToList();
            Assert.AreEqual(1, dv010s.Count);
        }

        [TestMethod]
        public async Task TestNamespace_ShouldNotReportLOC010()
        {
            var source = @"
namespace MyApp.Tests.Unit
{
    class Button { public string Text { get; set; } }
    class TestClass
    {
        void TestMethod()
        {
            var button = new Button();
            button.Text = ""OK"";
        }
    }
}";
            var diagnostics = await GetDiagnostics(source, new Analyzers.DisplayStringAnalyzer());
            var dv010s = diagnostics.Where(d => d.Id == "LOC010").ToList();
            Assert.AreEqual(0, dv010s.Count);
        }

        [TestMethod]
        public async Task TestUtilitiesNamespace_ShouldReportLOC010()
        {
            var source = @"
namespace MyApp.TestUtilities.Helpers
{
    class Button { public string Text { get; set; } }
    class TestClass
    {
        void TestMethod()
        {
            var button = new Button();
            button.Text = ""OK"";
        }
    }
}";
            var diagnostics = await GetDiagnostics(source, new Analyzers.DisplayStringAnalyzer());
            var dv010s = diagnostics.Where(d => d.Id == "LOC010").ToList();
            Assert.AreEqual(1, dv010s.Count);
        }

        [TestMethod]
        public async Task NonTestFileWithTestInName_ShouldReportLOC010()
        {
            var source = @"
namespace MyApp.Helpers
{
    class Button { public string Text { get; set; } }
    class TestClass
    {
        void TestMethod()
        {
            var button = new Button();
            button.Text = ""OK"";
        }
    }
}";
            var diagnostics = await GetDiagnostics(source, new Analyzers.DisplayStringAnalyzer(), "TestHelper.cs");
            var dv010s = diagnostics.Where(d => d.Id == "LOC010").ToList();
            Assert.AreEqual(1, dv010s.Count);
        }

        [TestMethod]
        public async Task GLLibraryReference_ShouldNotReportLOC010()
        {
            var source = @"
class GL { public static class Library { public static string S_HH_Limit = ""limit""; } }
class Label { public string Text { get; set; } }
class TestClass
{
    void TestMethod()
    {
        var label = new Label();
        label.Text = GL.Library.S_HH_Limit;
    }
}";
            var diagnostics = await GetDiagnostics(source, new Analyzers.DisplayStringAnalyzer());
            var dv010s = diagnostics.Where(d => d.Id == "LOC010").ToList();
            Assert.AreEqual(0, dv010s.Count);
        }

        [TestMethod]
        public async Task GLResxReference_ShouldNotReportLOC010()
        {
            var source = @"
class GL { public static class Resx { public static string S_Welcome = ""welcome""; } }
class Label { public string Text { get; set; } }
class TestClass
{
    void TestMethod()
    {
        var label = new Label();
        label.Text = GL.Resx.S_Welcome;
    }
}";
            var diagnostics = await GetDiagnostics(source, new Analyzers.DisplayStringAnalyzer());
            var dv010s = diagnostics.Where(d => d.Id == "LOC010").ToList();
            Assert.AreEqual(0, dv010s.Count);
        }

        [TestMethod]
        public async Task UnknownLibraryPrefix_ShouldReportLOC010()
        {
            var source = @"
class CustomLib { public static string S_HH_Limit = ""limit""; }
class Label { public string Text { get; set; } }
class TestClass
{
    void TestMethod()
    {
        var label = new Label();
        label.Text = CustomLib.S_HH_Limit;
    }
}";
            var diagnostics = await GetDiagnostics(source, new Analyzers.DisplayStringAnalyzer());
            var dv010s = diagnostics.Where(d => d.Id == "LOC010").ToList();
            Assert.AreEqual(1, dv010s.Count);
        }
    }
}
