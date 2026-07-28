using DataBank.Cli.Helpers;

namespace DataBank.Cli.Tests;

public class FileDetectorTests : IDisposable
{
    private readonly string _testDir;

    public FileDetectorTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"FileDetectorTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDir))
            Directory.Delete(_testDir, true);
    }

    [Theory]
    [InlineData("test.resx", "resx")]
    [InlineData("test.rc", "rc")]
    [InlineData("test.fhx", "fhx")]
    [InlineData("test.ahc", "ahc")]
    [InlineData("test.json", "json")]
    [InlineData("test.grf", "grf")]
    public void DetectFormat_ByExtension_ReturnsCorrectFormat(string fileName, string expected)
    {
        var filePath = Path.Combine(_testDir, fileName);
        File.WriteAllText(filePath, "content");

        var result = FileDetector.DetectFormat(filePath);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("test.txt")]
    [InlineData("test.xml")]
    [InlineData("test.csv")]
    [InlineData("test.log")]
    public void DetectFormat_UnsupportedExtension_ReturnsNull(string fileName)
    {
        var filePath = Path.Combine(_testDir, fileName);
        File.WriteAllText(filePath, "content");

        var result = FileDetector.DetectFormat(filePath);

        Assert.Null(result);
    }

    [Fact]
    public void DetectFormat_TxtFileInFhxDir_ReturnsFhx()
    {
        var fhxDir = Path.Combine(_testDir, "Fhx");
        Directory.CreateDirectory(fhxDir);
        var filePath = Path.Combine(fhxDir, "AlarmWords.txt");
        File.WriteAllText(filePath, "some content");

        var result = FileDetector.DetectFormat(filePath);

        Assert.Equal("fhx", result);
    }

    [Fact]
    public void DetectFormat_FhxFileInFhxDir_ReturnsFhx()
    {
        var fhxDir = Path.Combine(_testDir, "Fhx");
        Directory.CreateDirectory(fhxDir);
        var filePath = Path.Combine(fhxDir, "alarmTypes.fhx");
        File.WriteAllText(filePath, "some content");

        var result = FileDetector.DetectFormat(filePath);

        Assert.Equal("fhx", result);
    }

    [Fact]
    public void DetectFormat_CaseInsensitiveDirMatch_ReturnsFhx()
    {
        var fhxDir = Path.Combine(_testDir, "fhx");
        Directory.CreateDirectory(fhxDir);
        var filePath = Path.Combine(fhxDir, "data.txt");
        File.WriteAllText(filePath, "some content");

        var result = FileDetector.DetectFormat(filePath);

        Assert.Equal("fhx", result);
    }

    [Fact]
    public void DetectFormat_TxtFileWithFhxContent_ReturnsFhx()
    {
        var filePath = Path.Combine(_testDir, "data.txt");
        File.WriteAllText(filePath, "@Key@\t\"context\"\tSome value");

        var result = FileDetector.DetectFormat(filePath);

        Assert.Equal("fhx", result);
    }

    [Fact]
    public void DetectFormat_TxtFileWithoutFhxContent_ReturnsNull()
    {
        var filePath = Path.Combine(_testDir, "data.txt");
        File.WriteAllText(filePath, "This is just a plain text file.");

        var result = FileDetector.DetectFormat(filePath);

        Assert.Null(result);
    }

    [Fact]
    public void DetectFormat_EmptyTxtFile_ReturnsNull()
    {
        var filePath = Path.Combine(_testDir, "empty.txt");
        File.WriteAllText(filePath, "");

        var result = FileDetector.DetectFormat(filePath);

        Assert.Null(result);
    }

    [Fact]
    public void DetectFormat_NonexistentFile_ChecksExtensionOnly()
    {
        var filePath = Path.Combine(_testDir, "missing.fhx");

        var result = FileDetector.DetectFormat(filePath);

        Assert.Equal("fhx", result);
    }

    [Fact]
    public void DiscoverFiles_MixedFormats_ReturnsAllDetected()
    {
        var subDir = Path.Combine(_testDir, "Sub");
        Directory.CreateDirectory(subDir);

        var fhxDir = Path.Combine(_testDir, "Fhx");
        Directory.CreateDirectory(fhxDir);

        File.WriteAllText(Path.Combine(_testDir, "test.resx"), "<root/>");
        File.WriteAllText(Path.Combine(subDir, "test.rc"), "TEXT");
        File.WriteAllText(Path.Combine(fhxDir, "AlarmWords.txt"), "@Key@\t\"ctx\"\tVal");
        File.WriteAllText(Path.Combine(_testDir, "readme.txt"), "plain text");

        var results = FileDetector.DiscoverFiles(_testDir);

        Assert.Equal(3, results.Count);
        Assert.Contains(results, r => r.format == "resx");
        Assert.Contains(results, r => r.format == "rc");
        Assert.Contains(results, r => r.format == "fhx");
    }

    [Fact]
    public void DiscoverFiles_WithFormatFilter_ReturnsOnlyMatching()
    {
        File.WriteAllText(Path.Combine(_testDir, "test.resx"), "<root/>");
        File.WriteAllText(Path.Combine(_testDir, "test.rc"), "TEXT");

        var results = FileDetector.DiscoverFiles(_testDir, formatFilter: "resx");

        Assert.Single(results);
        Assert.Equal("resx", results[0].format);
    }

    [Fact]
    public void DiscoverFiles_EmptyDirectory_ReturnsEmpty()
    {
        var results = FileDetector.DiscoverFiles(_testDir);

        Assert.Empty(results);
    }

    [Fact]
    public void DiscoverFiles_NonexistentDirectory_ReturnsEmpty()
    {
        var results = FileDetector.DiscoverFiles(Path.Combine(_testDir, "nonexistent"));

        Assert.Empty(results);
    }
}
