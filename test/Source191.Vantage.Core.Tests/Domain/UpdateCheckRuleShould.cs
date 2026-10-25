using Source191.Vantage.Domain;

namespace Source191.Vantage.Core.Tests.Domain;

public sealed class UpdateCheckRuleShould
{
    private static readonly DateTimeOffset Now = new(2026, 10, 25, 12, 0, 0, TimeSpan.Zero);
    private static readonly ToolVersion Release = new("0.9.0");

    [Fact]
    public void CheckWhenNeverCheckedOrADayOld()
    {
        UpdateCheckRule.ShouldCheck(Release, enabled: true, lastCheckedAt: null, Now).ShouldBeTrue();
        UpdateCheckRule.ShouldCheck(Release, enabled: true, Now.AddHours(-24), Now).ShouldBeTrue();
        UpdateCheckRule.ShouldCheck(Release, enabled: true, Now.AddDays(-3), Now).ShouldBeTrue();
    }

    [Fact]
    public void NotCheckWithinADay()
    {
        UpdateCheckRule.ShouldCheck(Release, enabled: true, Now.AddHours(-23).AddMinutes(-59), Now).ShouldBeFalse();
        UpdateCheckRule.ShouldCheck(Release, enabled: true, Now, Now).ShouldBeFalse();
    }

    [Theory]
    [InlineData("0.10.0-local")]
    [InlineData("0.4.10-ci.12")]
    [InlineData("unknown")]
    public void NeverCheckAPrereleaseOrAnUnknownVersion(string running)
    {
        UpdateCheckRule.ShouldCheck(new ToolVersion(running), enabled: true, lastCheckedAt: null, Now).ShouldBeFalse();
    }

    [Fact]
    public void NeverCheckWhenTurnedOff()
    {
        UpdateCheckRule.ShouldCheck(Release, enabled: false, lastCheckedAt: null, Now).ShouldBeFalse();
    }

    [Fact]
    public void TakeTheNewestRelease()
    {
        UpdateCheckRule.Newest(["0.7.2", "0.10.0", "0.9.0", "0.11.0-rc.1", "not a version"])!.ToString().ShouldBe("0.10.0");
    }

    [Fact]
    public void FindNoNewestReleaseAmongPrereleasesAndNoise()
    {
        UpdateCheckRule.Newest(["0.11.0-rc.1", "rubbish"]).ShouldBeNull();
        UpdateCheckRule.Newest([]).ShouldBeNull();
    }

    [Fact]
    public void OfferOnlyAHigherRelease()
    {
        UpdateCheckRule.Offer(Release, enabled: true, "0.10.0").ShouldBe(new UpdateStatus.Available(ReleaseVersion.TryParse("0.9.0")!, ReleaseVersion.TryParse("0.10.0")!));
        UpdateCheckRule.Offer(Release, enabled: true, "0.9.0").ShouldBe(new UpdateStatus.UpToDate());
        UpdateCheckRule.Offer(Release, enabled: true, "0.8.0").ShouldBe(new UpdateStatus.UpToDate());
        UpdateCheckRule.Offer(Release, enabled: true, newest: null).ShouldBe(new UpdateStatus.UpToDate());
        UpdateCheckRule.Offer(Release, enabled: true, "rubbish").ShouldBe(new UpdateStatus.UpToDate());
    }

    [Fact]
    public void OfferNothingWhenTurnedOffOrForAPrerelease()
    {
        UpdateCheckRule.Offer(Release, enabled: false, "0.10.0").ShouldBe(new UpdateStatus.Off());
        UpdateCheckRule.Offer(new ToolVersion("0.9.0-local"), enabled: true, "0.10.0").ShouldBe(new UpdateStatus.Off());
        UpdateCheckRule.Offer(new ToolVersion("unknown"), enabled: true, "0.10.0").ShouldBe(new UpdateStatus.Off());
    }
}
