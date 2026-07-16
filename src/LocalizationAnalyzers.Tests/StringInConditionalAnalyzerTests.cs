using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LocalizationAnalyzers.Tests
{
    [TestClass]
    public class StringInConditionalAnalyzerTests
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
        public async Task IfStatementWithStringLiteral_ShouldReportLOC001()
        {
            var source = @"
class TestClass
{
    void TestMethod()
    {
        string status = ""Running"";
        if (status == ""Running"")
        {
        }
    }
}";
            var compilation = CreateCompilation(source);
            var analyzer = new Analyzers.StringInConditionalAnalyzer();
            var compWithAnalyzers = compilation.WithAnalyzers(new[] { (Microsoft.CodeAnalysis.Diagnostics.DiagnosticAnalyzer)analyzer }.ToImmutableArray());
            var diagnostics = await compWithAnalyzers.GetAnalyzerDiagnosticsAsync();
            var dv001s = diagnostics.Where(d => d.Id == "LOC001").ToList();
            Assert.AreEqual(1, dv001s.Count);
            Assert.IsTrue(dv001s[0].GetMessage().Contains("Running"));
        }

        [TestMethod]
        public async Task IfStatementWithStringLiteralEquals_ShouldReportLOC001()
        {
            var source = @"
class TestClass
{
    void TestMethod()
    {
        string status = ""Running"";
        if (status.Equals(""Running""))
        {
        }
    }
}";
            var compilation = CreateCompilation(source);
            var analyzer = new Analyzers.StringInConditionalAnalyzer();
            var compWithAnalyzers = compilation.WithAnalyzers(new[] { (Microsoft.CodeAnalysis.Diagnostics.DiagnosticAnalyzer)analyzer }.ToImmutableArray());
            var diagnostics = await compWithAnalyzers.GetAnalyzerDiagnosticsAsync();
            var dv001s = diagnostics.Where(d => d.Id == "LOC001").ToList();
            Assert.AreEqual(1, dv001s.Count);
        }

        [TestMethod]
        public async Task SwitchStatementWithStringCase_ShouldReportLOC001()
        {
            var source = @"
class TestClass
{
    void TestMethod()
    {
        string status = ""Running"";
        switch (status)
        {
            case ""Running"":
                break;
        }
    }
}";
            var compilation = CreateCompilation(source);
            var analyzer = new Analyzers.StringInConditionalAnalyzer();
            var compWithAnalyzers = compilation.WithAnalyzers(new[] { (Microsoft.CodeAnalysis.Diagnostics.DiagnosticAnalyzer)analyzer }.ToImmutableArray());
            var diagnostics = await compWithAnalyzers.GetAnalyzerDiagnosticsAsync();
            var dv001s = diagnostics.Where(d => d.Id == "LOC001").ToList();
            Assert.AreEqual(1, dv001s.Count);
        }

        [TestMethod]
        public async Task TernaryExpressionWithStringLiteral_ShouldReportLOC001()
        {
            var source = @"
class TestClass
{
    void TestMethod()
    {
        string status = ""Running"";
        var result = status == ""Running"" ? ""Yes"" : ""No"";
    }
}";
            var compilation = CreateCompilation(source);
            var analyzer = new Analyzers.StringInConditionalAnalyzer();
            var compWithAnalyzers = compilation.WithAnalyzers(new[] { (Microsoft.CodeAnalysis.Diagnostics.DiagnosticAnalyzer)analyzer }.ToImmutableArray());
            var diagnostics = await compWithAnalyzers.GetAnalyzerDiagnosticsAsync();
            var dv001s = diagnostics.Where(d => d.Id == "LOC001").ToList();
            Assert.AreEqual(3, dv001s.Count);
        }

        [TestMethod]
        public async Task StringInLambda_ShouldReportLOC001()
        {
            var source = @"
using System;
class TestClass
{
    void TestMethod()
    {
        Action action = () =>
        {
            string status = ""Running"";
            if (status == ""Running"")
            {
            }
        };
    }
}";
            var compilation = CreateCompilation(source);
            var analyzer = new Analyzers.StringInConditionalAnalyzer();
            var compWithAnalyzers = compilation.WithAnalyzers(new[] { (Microsoft.CodeAnalysis.Diagnostics.DiagnosticAnalyzer)analyzer }.ToImmutableArray());
            var diagnostics = await compWithAnalyzers.GetAnalyzerDiagnosticsAsync();
            var dv001s = diagnostics.Where(d => d.Id == "LOC001").ToList();
            Assert.AreEqual(1, dv001s.Count);
        }

        [TestMethod]
        public async Task NoStringLiteral_ShouldNotReport()
        {
            var source = @"
class TestClass
{
    void TestMethod()
    {
        string status = ""Running"";
        if (status == GetStatus())
        {
        }
    }
}";
            var compilation = CreateCompilation(source);
            var analyzer = new Analyzers.StringInConditionalAnalyzer();
            var compWithAnalyzers = compilation.WithAnalyzers(new[] { (Microsoft.CodeAnalysis.Diagnostics.DiagnosticAnalyzer)analyzer }.ToImmutableArray());
            var diagnostics = await compWithAnalyzers.GetAnalyzerDiagnosticsAsync();
            var dv001s = diagnostics.Where(d => d.Id == "LOC001").ToList();
            Assert.AreEqual(0, dv001s.Count);
        }

        [TestMethod]
        public async Task EmptyStringInConditional_ShouldNotReport()
        {
            var source = @"
class TestClass
{
    void TestMethod()
    {
        string val = ""hello"";
        var result = val == """" ? ""yes"" : ""no"";
    }
}";
            var compilation = CreateCompilation(source);
            var analyzer = new Analyzers.StringInConditionalAnalyzer();
            var compWithAnalyzers = compilation.WithAnalyzers(new[] { (Microsoft.CodeAnalysis.Diagnostics.DiagnosticAnalyzer)analyzer }.ToImmutableArray());
            var diagnostics = await compWithAnalyzers.GetAnalyzerDiagnosticsAsync();
            var dv001s = diagnostics.Where(d => d.Id == "LOC001").ToList();
            Assert.AreEqual(2, dv001s.Count);
            Assert.IsTrue(dv001s.Any(d => d.GetMessage().Contains("yes")));
            Assert.IsTrue(dv001s.Any(d => d.GetMessage().Contains("no")));
        }

        [TestMethod]
        public async Task CommaStringInTernary_ShouldNotReport()
        {
            var source = @"
class TestClass
{
    void TestMethod()
    {
        int count = 5;
        var comma = count > 1 ? "","" : """";
    }
}";
            var compilation = CreateCompilation(source);
            var analyzer = new Analyzers.StringInConditionalAnalyzer();
            var compWithAnalyzers = compilation.WithAnalyzers(new[] { (Microsoft.CodeAnalysis.Diagnostics.DiagnosticAnalyzer)analyzer }.ToImmutableArray());
            var diagnostics = await compWithAnalyzers.GetAnalyzerDiagnosticsAsync();
            var dv001s = diagnostics.Where(d => d.Id == "LOC001").ToList();
            Assert.AreEqual(0, dv001s.Count);
        }

        [TestMethod]
        public async Task SingleCharStringInConditional_ShouldNotReport()
        {
            var source = @"
class TestClass
{
    void TestMethod()
    {
        string val = ""hello"";
        var result = val == "" "" ? ""space"" : ""other"";
    }
}";
            var compilation = CreateCompilation(source);
            var analyzer = new Analyzers.StringInConditionalAnalyzer();
            var compWithAnalyzers = compilation.WithAnalyzers(new[] { (Microsoft.CodeAnalysis.Diagnostics.DiagnosticAnalyzer)analyzer }.ToImmutableArray());
            var diagnostics = await compWithAnalyzers.GetAnalyzerDiagnosticsAsync();
            var dv001s = diagnostics.Where(d => d.Id == "LOC001").ToList();
            Assert.AreEqual(2, dv001s.Count);
            Assert.IsTrue(dv001s.Any(d => d.GetMessage().Contains("space")));
            Assert.IsTrue(dv001s.Any(d => d.GetMessage().Contains("other")));
        }
    }
}
