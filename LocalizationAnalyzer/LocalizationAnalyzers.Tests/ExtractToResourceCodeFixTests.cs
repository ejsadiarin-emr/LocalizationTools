using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using LocalizationAnalyzers;
using LocalizationAnalyzers.CodeFixes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LocalizationAnalyzers.Tests
{
    [TestClass]
    public class ExtractToResourceCodeFixTests
    {
        private static readonly MetadataReference CorlibReference = MetadataReference.CreateFromFile(typeof(object).Assembly.Location);
        private static readonly MetadataReference SystemReference = MetadataReference.CreateFromFile(typeof(Console).Assembly.Location);

        private static CSharpCompilation CreateCompilation(string source)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(source);
            return CSharpCompilation.Create(
                "TestAssembly",
                new[] { syntaxTree },
                new[] { CorlibReference, SystemReference },
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        }

        [TestMethod]
        public async Task CodeFix_ProviderIsRegistered()
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
            var compilation = CreateCompilation(source);
            var codeFixProvider = new ExtractToResourceCodeFix();

            // Verify the code fix provider has the correct fixable diagnostic IDs
            var fixableIds = codeFixProvider.FixableDiagnosticIds;
            CollectionAssert.Contains(fixableIds, "LOC010");
        }

        [TestMethod]
        public void CodeFix_KeyGeneration_CorrectFormat()
        {
            var codeFix = new ExtractToResourceCodeFix();
            var key = codeFix.GenerateKey(null, null, "Hello");
            Assert.IsTrue(key.Contains("hello"));
        }

        [TestMethod]
        public void CodeFix_KeyGeneration_SlugifiesSpaces()
        {
            var codeFix = new ExtractToResourceCodeFix();
            var key = codeFix.GenerateKey(null, null, "Start Pump");
            Assert.IsTrue(key.Contains("startpump"));
        }
    }
}
