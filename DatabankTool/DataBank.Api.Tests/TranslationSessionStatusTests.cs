using DataBank.Api.Models;

namespace DataBank.Api.Tests;

public class TranslationSessionStatusTests
{
    [Fact]
    public void PendingToInProgress_IsValid()
    {
        Assert.True(TranslationSessionStatus.IsValidTransition("pending", "in-progress"));
    }

    [Fact]
    public void InProgressToCompleted_IsValid()
    {
        Assert.True(TranslationSessionStatus.IsValidTransition("in-progress", "completed"));
    }

    [Fact]
    public void PendingToCompleted_IsInvalid()
    {
        Assert.False(TranslationSessionStatus.IsValidTransition("pending", "completed"));
    }

    [Fact]
    public void CompletedToPending_IsInvalid()
    {
        Assert.False(TranslationSessionStatus.IsValidTransition("completed", "pending"));
    }

    [Fact]
    public void CompletedToInProgress_IsInvalid()
    {
        Assert.False(TranslationSessionStatus.IsValidTransition("completed", "in-progress"));
    }

    [Fact]
    public void InProgressToPending_IsInvalid()
    {
        Assert.False(TranslationSessionStatus.IsValidTransition("in-progress", "pending"));
    }

    [Theory]
    [InlineData("pending", "in-progress", true)]
    [InlineData("in-progress", "completed", true)]
    [InlineData("pending", "completed", false)]
    [InlineData("completed", "pending", false)]
    [InlineData("completed", "in-progress", false)]
    [InlineData("in-progress", "pending", false)]
    public void IsValidTransition_ReturnsExpected(string from, string to, bool expected)
    {
        Assert.Equal(expected, TranslationSessionStatus.IsValidTransition(from, to));
    }
}
