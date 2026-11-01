using Microsoft.Extensions.Time.Testing;
using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;
using Source191.Vantage.Tui;

namespace Source191.Vantage.Core.Tests.Tui;

public sealed class ConfigScreenDefinitionShould
{
    [Fact]
    public void SayTheLastUpdateCheckInTheClocksLocalZone()
    {
        FakeTimeProvider clock = new(new DateTimeOffset(2026, 7, 15, 10, 0, 0, TimeSpan.Zero));
        clock.SetLocalTimeZone(TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time"));
        ConfigRow row = new ConfigRow.Setup(SetupItem.LastUpdateCheck, new SetupState.Checked(new DateTimeOffset(2026, 7, 15, 8, 12, 0, TimeSpan.Zero), new NewestRelease.Found(ReleaseVersions.Version("0.18.0"))));

        string value = new ConfigScreenDefinition(clock).Cell(row, 1, clock.GetUtcNow(), 200);

        value.ShouldBe("09:12 today, newest 0.18.0");
    }
}