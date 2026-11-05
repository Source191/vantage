using Source191.Torni.Application;

namespace Source191.Torni.Core.Tests.Application;

public sealed class ReleasePageShould
{
    [Fact]
    public void BuildTheUrlFromTheVersionAlone()
    {
        ReleasePage.For(ReleaseVersions.Version("0.21.0")).AbsoluteUri.ShouldBe("https://github.com/Source191/torni/releases/tag/v0.21.0");
    }
}