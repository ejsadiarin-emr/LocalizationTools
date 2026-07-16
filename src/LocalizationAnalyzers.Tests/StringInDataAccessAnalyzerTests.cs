using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LocalizationAnalyzers.Tests
{
    [TestClass]
    public class StringInDataAccessAnalyzerTests
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

        private static async Task<List<Diagnostic>> GetDiagnostics(string source, Analyzers.StringInDataAccessAnalyzer analyzer)
        {
            var compilation = CreateCompilation(source);
            var compWithAnalyzers = compilation.WithAnalyzers(new[] { (Microsoft.CodeAnalysis.Diagnostics.DiagnosticAnalyzer)analyzer }.ToImmutableArray());
            return (await compWithAnalyzers.GetAnalyzerDiagnosticsAsync()).ToList();
        }

        [TestMethod]
        public async Task FindMethodWithStringLiteral_ShouldReportLOC002()
        {
            var source = @"
class TestClass
{
    void TestMethod()
    {
        var result = Find(""test"");
    }
}";
            var diagnostics = await GetDiagnostics(source, new Analyzers.StringInDataAccessAnalyzer());
            var dv002s = diagnostics.Where(d => d.Id == "LOC002").ToList();
            Assert.AreEqual(1, dv002s.Count);
            Assert.IsTrue(dv002s[0].GetMessage().Contains("test"));
        }

        [TestMethod]
        public async Task GetMethodWithStringLiteral_ShouldReportLOC002()
        {
            var source = @"
class TestClass
{
    void TestMethod()
    {
        var result = Get(""test"");
    }
}";
            var diagnostics = await GetDiagnostics(source, new Analyzers.StringInDataAccessAnalyzer());
            var dv002s = diagnostics.Where(d => d.Id == "LOC002").ToList();
            Assert.AreEqual(1, dv002s.Count);
        }

        [TestMethod]
        public async Task QueryMethodWithStringLiteral_ShouldReportLOC002()
        {
            var source = @"
class TestClass
{
    void TestMethod()
    {
        var result = Query(""test"");
    }
}";
            var diagnostics = await GetDiagnostics(source, new Analyzers.StringInDataAccessAnalyzer());
            var dv002s = diagnostics.Where(d => d.Id == "LOC002").ToList();
            Assert.AreEqual(1, dv002s.Count);
        }

        [TestMethod]
        public async Task DatabaseContextMethod_ShouldReportLOC002()
        {
            var source = @"
class TestClass
{
    void TestMethod()
    {
        var result = DbContext.Find(""test"");
    }
}";
            var diagnostics = await GetDiagnostics(source, new Analyzers.StringInDataAccessAnalyzer());
            var dv002s = diagnostics.Where(d => d.Id == "LOC002").ToList();
            Assert.AreEqual(1, dv002s.Count);
        }

        [TestMethod]
        public async Task RepositoryMethod_ShouldReportLOC002()
        {
            var source = @"
class TestClass
{
    void TestMethod()
    {
        var result = UserRepository.Get(""test"");
    }
}";
            var diagnostics = await GetDiagnostics(source, new Analyzers.StringInDataAccessAnalyzer());
            var dv002s = diagnostics.Where(d => d.Id == "LOC002").ToList();
            Assert.AreEqual(1, dv002s.Count);
        }

        [TestMethod]
        public async Task NonDataAccessMethod_ShouldNotReport()
        {
            var source = @"
class TestClass
{
    void TestMethod()
    {
        var result = ToString(""test"");
    }
}";
            var diagnostics = await GetDiagnostics(source, new Analyzers.StringInDataAccessAnalyzer());
            var dv002s = diagnostics.Where(d => d.Id == "LOC002").ToList();
            Assert.AreEqual(0, dv002s.Count);
        }

        [TestMethod]
        public async Task GetHashCode_ShouldNotReportLOC002()
        {
            var source = @"
class TestClass
{
    void TestMethod()
    {
        var result = GetHashCode(""test"");
    }
}";
            var diagnostics = await GetDiagnostics(source, new Analyzers.StringInDataAccessAnalyzer());
            var dv002s = diagnostics.Where(d => d.Id == "LOC002").ToList();
            Assert.AreEqual(0, dv002s.Count);
        }

        [TestMethod]
        public async Task GetType_ShouldNotReportLOC002()
        {
            var source = @"
class TestClass
{
    void TestMethod()
    {
        var result = GetType(""test"");
    }
}";
            var diagnostics = await GetDiagnostics(source, new Analyzers.StringInDataAccessAnalyzer());
            var dv002s = diagnostics.Where(d => d.Id == "LOC002").ToList();
            Assert.AreEqual(0, dv002s.Count);
        }

        [TestMethod]
        public async Task StringConcatenation_ShouldNotReportLOC002()
        {
            var source = @"
class TestClass
{
    void TestMethod()
    {
        var result = ""hello"" + ""world"";
    }
}";
            var diagnostics = await GetDiagnostics(source, new Analyzers.StringInDataAccessAnalyzer());
            var dv002s = diagnostics.Where(d => d.Id == "LOC002").ToList();
            Assert.AreEqual(0, dv002s.Count);
        }

        [TestMethod]
        public async Task DataAccessMethodWithNonStringArgument_ShouldNotReport()
        {
            var source = @"
class TestClass
{
    void TestMethod()
    {
        var result = Find(123);
    }
}";
            var diagnostics = await GetDiagnostics(source, new Analyzers.StringInDataAccessAnalyzer());
            var dv002s = diagnostics.Where(d => d.Id == "LOC002").ToList();
            Assert.AreEqual(0, dv002s.Count);
        }
    }
}
