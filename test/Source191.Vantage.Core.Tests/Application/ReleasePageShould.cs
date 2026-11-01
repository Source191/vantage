using Source191.Vantage.Application;

namespace Source191.Vantage.Core.Tests.Application;

public sealed class ReleasePageShould
{
    [Fact]
    public void BuildTheUrlFromTheVersionAlone()
    {
        ReleasePage.For(ReleaseVersions.Version("0.21.0")).AbsoluteUri.ShouldBe("https://github.com/Source191/vantage/releases/tag/v0.21.0");
    }
}