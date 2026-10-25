using Source191.Vantage.Domain;

namespace Source191.Vantage.Core.Tests.Domain;

public sealed class ToolVersionShould
{
    [Fact]
    public void CutTheCommitAfterThePlus()
    {
        ToolVersion.FromInformational("0.5.0+3f2a9c1d").Text.ShouldBe("0.5.0");
    }

    [Fact]
    public void ShortenALocalPrereleaseToLocal()
    {
        ToolVersion.FromInformational("0.5.0-local.20261025T101500+abc").Text.ShouldBe("0.5.0-local");
    }

    [Fact]
    public void KeepAnOrdinaryPrerelease()
    {
        ToolVersion.FromInformational("0.6.0-beta.2+abc").Text.ShouldBe("0.6.0-beta.2");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("+abc")]
    public void ReadUnknownWhenBlank(string? informational)
    {
        ToolVersion.FromInformational(informational).Text.ShouldBe("unknown");
    }
}