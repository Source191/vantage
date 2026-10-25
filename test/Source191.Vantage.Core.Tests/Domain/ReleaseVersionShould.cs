using Source191.Vantage.Domain;

namespace Source191.Vantage.Core.Tests.Domain;

public sealed class ReleaseVersionShould
{
    [Theory]
    [InlineData("0.10.0", "0.9.0")]
    [InlineData("1.0.0", "0.99.99")]
    [InlineData("0.9.1", "0.9.0")]
    [InlineData("1.0.0", "1.0.0-rc.1")]
    [InlineData("1.0.0-rc.2", "1.0.0-rc.1")]
    [InlineData("1.0.0-rc.10", "1.0.0-rc.9")]
    [InlineData("1.0.0-rc.1", "1.0.0-rc")]
    [InlineData("1.0.0-beta", "1.0.0-alpha")]
    [InlineData("1.0.0-alpha", "1.0.0-1")]
    public void CompareBySemanticVersionPrecedence(string higher, string lower)
    {
        ReleaseVersion high = ReleaseVersion.TryParse(higher)!;
        ReleaseVersion low = ReleaseVersion.TryParse(lower)!;

        high.IsHigherThan(low).ShouldBeTrue();
        low.IsHigherThan(high).ShouldBeFalse();
        high.CompareTo(low).ShouldBePositive();
        low.CompareTo(high).ShouldBeNegative();
    }

    [Fact]
    public void IgnoreBuildMetadata()
    {
        ReleaseVersion withMetadata = ReleaseVersion.TryParse("0.9.0+3f2a9c1d")!;

        withMetadata.CompareTo(ReleaseVersion.TryParse("0.9.0")!).ShouldBe(0);
        withMetadata.ToString().ShouldBe("0.9.0");
    }

    [Theory]
    [InlineData("0.10.0-local")]
    [InlineData("0.4.10-ci.12")]
    public void KnowAPrerelease(string text)
    {
        ReleaseVersion.TryParse(text)!.IsPrerelease.ShouldBeTrue();
    }

    [Fact]
    public void KnowARelease()
    {
        ReleaseVersion.TryParse("0.10.0")!.IsPrerelease.ShouldBeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("unknown")]
    [InlineData("1.0")]
    [InlineData("1.0.0.0")]
    [InlineData("1.x.0")]
    [InlineData("-1.0.0")]
    [InlineData("1.0.0-")]
    public void AnswerNullForTextThatIsNotAVersion(string? text)
    {
        ReleaseVersion.TryParse(text).ShouldBeNull();
    }

    [Fact]
    public void WriteItselfAsItWasRead()
    {
        ReleaseVersion.TryParse("0.10.0-rc.1")!.ToString().ShouldBe("0.10.0-rc.1");
    }
}