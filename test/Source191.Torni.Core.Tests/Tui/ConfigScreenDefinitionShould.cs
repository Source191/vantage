using Microsoft.Extensions.Time.Testing;
using Source191.Torni.Application;
using Source191.Torni.Application.Views;
using Source191.Torni.Domain;
using Source191.Torni.Tui;

namespace Source191.Torni.Core.Tests.Tui;

public sealed class ConfigScreenDefinitionShould
{
    [Fact]
    public void SayTheLastUpdateCheckInTheClocksLocalZone()
    {
        FakeTimeProvider clock = new(new DateTimeOffset(2026, 7, 15, 10, 0, 0, TimeSpan.Zero));
        clock.SetLocalTimeZone(TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time"));
        ConfigRow row = new ConfigRow.Setup(SetupItem.LastUpdateCheck, new SetupState.Checked(new DateTimeOffset(2026, 7, 15, 8, 12, 0, TimeSpan.Zero), new NewestRelease.Found(ReleaseVersions.Version("0.18.0"))));

        string value = new ConfigScreenDefinition(clock).Columns[1].Cell(row, clock.GetUtcNow(), 200);

        value.ShouldBe("09:12 today, newest 0.18.0");
    }

    [Fact]
    public void ListItsColumnsAsWordingDoesWithValueElastic()
    {
        ConfigScreenDefinition definition = new(new FakeTimeProvider());

        definition.Columns.Select(column => column.Help).ShouldBe(Wording.ConfigColumns);
        definition.Columns.Single(column => column.Fit is ColumnFit.Elastic).Help.ShouldBeSameAs(Wording.ValueColumn);
    }

    [Fact]
    public void DrawTheSetupHeadingInOrangeAndTheSetupGrey()
    {
        ConfigScreenDefinition definition = new(new FakeTimeProvider());

        definition.RowColour(new ConfigRow.Heading()).ShouldBe(Palette.Orange);
        definition.RowColour(new ConfigRow.Setup(SetupItem.ClaudeBell, new SetupState.Enabled())).ShouldBe(Palette.Grey);
        definition.RowColour(new ConfigRow.CheckForUpdates(On: true)).ShouldBe(Palette.White);
    }

    [Fact]
    public void ElideALongValueInItsMiddle()
    {
        FakeTimeProvider clock = new(new DateTimeOffset(2026, 7, 15, 10, 0, 0, TimeSpan.Zero));
        ConfigRow row = new ConfigRow.Setup(SetupItem.LastUpdateCheck, new SetupState.Checked(new DateTimeOffset(2026, 7, 15, 8, 12, 0, TimeSpan.Zero), new NewestRelease.Found(ReleaseVersions.Version("0.18.0"))));

        string value = new ConfigScreenDefinition(clock).Columns[1].Cell(row, clock.GetUtcNow(), 10);

        value.ShouldBe("08:12…18.0");
    }

    [Fact]
    public void NameItsTab()
    {
        new ConfigScreenDefinition(new FakeTimeProvider()).Tab.ShouldBe(ConsoleTab.Config);
    }

    [Fact]
    public void NeverRefreshOnATimer()
    {
        new ConfigScreenDefinition(new FakeTimeProvider()).Refresh.ShouldBe(new TabRefresh.Never());
    }
}