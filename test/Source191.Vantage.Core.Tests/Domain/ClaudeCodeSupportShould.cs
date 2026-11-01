using Source191.Vantage.Domain;

namespace Source191.Vantage.Core.Tests.Domain;

public sealed class ClaudeCodeSupportShould
{
    [Fact]
    public void AnswerUnknownWhenNothingWasRead()
    {
        ClaudeCodeSupport.Check(null).ShouldBe(new ClaudeCodeVersion.Unknown());
    }

    [Fact]
    public void AnswerBelowMinimumForAnOlderVersion()
    {
        ClaudeCodeSupport.Check("2.1.302").ShouldBe(new ClaudeCodeVersion.BelowMinimum(new ReleaseVersion(2, 1, 302, ""), new ReleaseVersion(2, 1, 309, "")));
    }

    [Theory]
    [InlineData("2.1.309")]
    [InlineData("2.1.310")]
    public void AnswerReportedAtOrAboveTheMinimum(string version)
    {
        ClaudeCodeSupport.Check(version).ShouldBe(new ClaudeCodeVersion.Reported(version));
    }

    [Fact]
    public void AnswerReportedForAVersionItCannotRead()
    {
        ClaudeCodeSupport.Check("2.1").ShouldBe(new ClaudeCodeVersion.Reported("2.1"));
    }
}