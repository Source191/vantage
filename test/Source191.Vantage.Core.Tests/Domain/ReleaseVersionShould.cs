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
        ReleaseVersion high = ReleaseVersions.Version(higher);
        ReleaseVersion low = ReleaseVersions.Version(lower);

        high.IsHigherThan(low).ShouldBeTrue();
        low.IsHigherThan(high).ShouldBeFalse();
        high.CompareTo(low).ShouldBePositive();
        low.CompareTo(high).ShouldBeNegative();
    }

    [Fact]
    public void CompareWithOperators()
    {
        ReleaseVersion low = ReleaseVersions.Version("0.9.0");
        ReleaseVersion high = ReleaseVersions.Version("0.10.0");
        ReleaseVersion same = ReleaseVersions.Version("0.9.0");

        (low < high).ShouldBeTrue();
        (high < low).ShouldBeFalse();
        (low < same).ShouldBeFalse();
        (low <= same).ShouldBeTrue();
        (high <= low).ShouldBeFalse();
        (high > low).ShouldBeTrue();
        (low > same).ShouldBeFalse();
        (low >= same).ShouldBeTrue();
        (low >= high).ShouldBeFalse();
    }

    [Fact]
    public void RankAboveNothing()
    {
        ReleaseVersions.Version("0.9.0").CompareTo(null).ShouldBePositive();
    }

    [Fact]
    public void IgnoreBuildMetadata()
    {
        ReleaseVersion withMetadata = ReleaseVersions.Version("0.9.0+3f2a9c1d");

        withMetadata.CompareTo(ReleaseVersions.Version("0.9.0")).ShouldBe(0);
        withMetadata.ToString().ShouldBe("0.9.0");
    }

    [Theory]
    [InlineData("0.10.0-local")]
    [InlineData("0.4.10-ci.12")]
    public void KnowAPrerelease(string text)
    {
        ReleaseVersions.Version(text).IsPrerelease.ShouldBeTrue();
    }

    [Fact]
    public void KnowARelease()
    {
        ReleaseVersions.Version("0.10.0").IsPrerelease.ShouldBeFalse();
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
    public void AnswerFalseForTextThatIsNotAVersion(string? text)
    {
        ReleaseVersion.TryParse(text, out ReleaseVersion? version).ShouldBeFalse();
        version.ShouldBeNull();
    }

    [Theory]
    [InlineData("0.10.0", 0, 10, 0, null)]
    [InlineData("2.1.310", 2, 1, 310, null)]
    [InlineData("0.19.0-preview.6", 0, 19, 0, "preview.6")]
    [InlineData(" 1.0.0+3f2a9c1d ", 1, 0, 0, null)]
    public void ReadAVersion(string text, int major, int minor, int patch, string? prerelease)
    {
        ReleaseVersion.TryParse(text, out ReleaseVersion? version).ShouldBeTrue();
        version.ShouldBe(new ReleaseVersion(major, minor, patch, prerelease));
    }

    [Fact]
    public void ReadOnlyTheTextsThatParse()
    {
        string[] texts = ["0.10.0", "unknown", "0.9.0-rc.1", "1.0", "0.11.0"];

        ReleaseVersion.Parsed(texts).ShouldBe([new ReleaseVersion(0, 10, 0, null), new ReleaseVersion(0, 9, 0, "rc.1"), new ReleaseVersion(0, 11, 0, null)]);
    }

    [Fact]
    public void WriteItselfAsItWasRead()
    {
        ReleaseVersions.Version("0.10.0-rc.1").ToString().ShouldBe("0.10.0-rc.1");
    }
}