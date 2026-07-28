using DataBank.Cli.Helpers;

namespace DataBank.Cli.Tests;

public class FileHelperTests
{
    [Theory]
    [InlineData("DVHExecutive-DNT.rc", true)]
    [InlineData("DVHExecutive-DNT.h", true)]
    [InlineData("file-dnt.txt", true)]
    [InlineData("file-DNT.txt", true)]
    [InlineData("file_Dnt.txt", true)]
    [InlineData("DNT-file.txt", true)]
    [InlineData("file.txt", false)]
    [InlineData("file-dont.txt", false)]
    [InlineData("DONT-file.txt", false)]
    public void HasDntInFilename_VariousPatterns_ReturnsExpected(string fileName, bool expected)
    {
        var result = FileHelper.HasDntInFilename($"/path/to/{fileName}");
        Assert.Equal(expected, result);
    }

    [Fact]
    public void HasDntInFilename_FullPath_ChecksOnlyFilename()
    {
        var result = FileHelper.HasDntInFilename("/some/DNT/directory/file.txt");
        Assert.False(result);
    }

    [Fact]
    public void HasDntInFilename_EmptyPath_ReturnsFalse()
    {
        var result = FileHelper.HasDntInFilename("");
        Assert.False(result);
    }
}