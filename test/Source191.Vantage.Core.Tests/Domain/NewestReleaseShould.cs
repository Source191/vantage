using Source191.Vantage.Domain;

namespace Source191.Vantage.Core.Tests.Domain;

public sealed class NewestReleaseShould
{
    [Fact]
    public void ReadARecordedVersionAsFound()
    {
        NewestRelease.Recorded("0.10.0").ShouldBe(new NewestRelease.Found(ReleaseVersions.Version("0.10.0")));
    }

    [Fact]
    public void ReadARecordedPrereleaseAsFound()
    {
        NewestRelease.Recorded("0.11.0-rc.1").ShouldBe(new NewestRelease.Found(ReleaseVersions.Version("0.11.0-rc.1")));
    }

    [Fact]
    public void ReadTextThatIsNotAVersionAsUnreadable()
    {
        NewestRelease.Recorded("rubbish").ShouldBe(new NewestRelease.Unreadable("rubbish"));
    }
}