using System.Text.Json;
using System.Text.Json.Serialization;
using DataBank.Cli.Models;

namespace DataBank.Cli.Tests;

public class IntegrationTests
{
    private static string SamplesDir => Path.Combine(
        Directory.GetCurrentDirectory(), "..", "..", "..", "..", "..", "l10n-files");

    [Fact]
    public void Cli_FullRun_ProducesValidOutput()
    {
        var outputPath = Path.Combine(Path.GetTempPath(), $"databank-test-{Guid.NewGuid():N}.json");

        try
        {
            var exitCode = DataBank.Cli.Program.Main([
                SamplesDir,
                "--output", outputPath,
                "--stats"
            ]);

            Assert.Equal(0, exitCode);
            Assert.True(File.Exists(outputPath));

            var json = File.ReadAllText(outputPath);
            var output = JsonSerializer.Deserialize<DataBankOutput>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter() }
            });

            Assert.NotNull(output);
            Assert.Equal(2, output.Version);
            Assert.NotEmpty(output.Generated);
            Assert.NotEmpty(output.Entries);

            // Should have entries from both formats
            Assert.Contains(output.Entries, e => e.Source.Format == "resx");
            Assert.Contains(output.Entries, e => e.Source.Format == "rc");

            // Should have multiple locales
            var locales = output.Entries.Select(e => e.Locale).Distinct().ToList();
            Assert.True(locales.Count >= 3, $"Expected at least 3 locales, got {locales.Count}");
        }
        finally
        {
            if (File.Exists(outputPath))
                File.Delete(outputPath);
        }
    }

    [Fact]
    public void Cli_FilterByFormat_OnlyReturnsMatchingEntries()
    {
        var outputPath = Path.Combine(Path.GetTempPath(), $"databank-test-{Guid.NewGuid():N}.json");

        try
        {
            DataBank.Cli.Program.Main([
                SamplesDir,
                "--format", "resx",
                "--output", outputPath
            ]);

            var json = File.ReadAllText(outputPath);
            var output = JsonSerializer.Deserialize<DataBankOutput>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter() }
            });

            Assert.NotNull(output);
            Assert.All(output.Entries, e => Assert.Equal("resx", e.Source.Format));
        }
        finally
        {
            if (File.Exists(outputPath))
                File.Delete(outputPath);
        }
    }

    [Fact]
    public void Cli_EmptyDirectory_ProducesNoEntries()
    {
        var emptyDir = Path.Combine(Path.GetTempPath(), $"databank-empty-{Guid.NewGuid():N}");
        var outputPath = Path.Combine(Path.GetTempPath(), $"databank-test-{Guid.NewGuid():N}.json");

        try
        {
            Directory.CreateDirectory(emptyDir);

            var exitCode = DataBank.Cli.Program.Main([
                emptyDir,
                "--output", outputPath
            ]);

            Assert.Equal(0, exitCode);
            Assert.False(File.Exists(outputPath));
        }
        finally
        {
            Directory.Delete(emptyDir, true);
            if (File.Exists(outputPath))
                File.Delete(outputPath);
        }
    }

    [Fact]
    public void Cli_FlagUntranslated_ProducesOutputWithTranslationFields()
    {
        var outputPath = Path.Combine(Path.GetTempPath(), $"databank-test-{Guid.NewGuid():N}.json");

        try
        {
            var exitCode = DataBank.Cli.Program.Main([
                SamplesDir,
                "--output", outputPath,
                "--flag-untranslated"
            ]);

            Assert.Equal(0, exitCode);
            Assert.True(File.Exists(outputPath));

            var json = File.ReadAllText(outputPath);
            var output = JsonSerializer.Deserialize<DataBankOutput>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter() }
            });

            Assert.NotNull(output);
            Assert.NotNull(output.TranslationSummary);

            Assert.NotEmpty(output.Entries);
            Assert.All(output.Entries, e =>
            {
                Assert.True(e.Metadata.IsTranslated || !e.Metadata.IsTranslated);
            });

            Assert.True(output.TranslationSummary.Overall.Translated >= 0);
            Assert.True(output.TranslationSummary.Overall.Untranslated >= 0);
            Assert.NotEmpty(output.TranslationSummary.ByLocale);
        }
        finally
        {
            if (File.Exists(outputPath))
                File.Delete(outputPath);
        }
    }

    [Fact]
    public void Cli_WithoutFlagUntranslated_SummaryIsNull()
    {
        var outputPath = Path.Combine(Path.GetTempPath(), $"databank-test-{Guid.NewGuid():N}.json");

        try
        {
            var exitCode = DataBank.Cli.Program.Main([
                SamplesDir,
                "--output", outputPath
            ]);

            Assert.Equal(0, exitCode);
            Assert.True(File.Exists(outputPath));

            var json = File.ReadAllText(outputPath);
            var output = JsonSerializer.Deserialize<DataBankOutput>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter() }
            });

            Assert.NotNull(output);
            Assert.Null(output.TranslationSummary);
        }
        finally
        {
            if (File.Exists(outputPath))
                File.Delete(outputPath);
        }
    }
}
