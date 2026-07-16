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

        private static async Task<List<Diagnostic>> GetDiagnostics(string source, Analyzers.DisplayStringAnalyzer analyzer)
        {
            var compilation = CreateCompilation(source);
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
    }
}
