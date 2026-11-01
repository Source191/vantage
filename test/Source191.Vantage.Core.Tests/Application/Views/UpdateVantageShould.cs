using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;

namespace Source191.Vantage.Core.Tests.Application.Views;

/// <summary>What the update key does with what the update check knows: ask with what changed, or say why there is nothing to do.</summary>
public sealed class UpdateVantageShould
{
    private const string Changelog = "## 0.21.0\n### Added\n- A key that opens the Northwind order (#171)\n## 0.20.0\n### Fixed\n- Renaming an order keeps its history (#169)\n## 0.19.0\n### Fixed\n- The supplier column no longer flickers (#150)\n";

    [Fact]
    public void AskWithTheNotesBetweenTheRunningAndNewestVersions()
    {
        UpdateStatus available = new UpdateStatus.Available(ReleaseVersions.Version("0.19.0"), ReleaseVersions.Version("0.21.0"));
        ReleaseNotes.TryParse(Changelog, out ReleaseNotes? notes).ShouldBeTrue();

        UpdateDecision.Ask ask = UpdateVantage.Decide(available, notes).ShouldBeOfType<UpdateDecision.Ask>();

        ask.Current.ShouldBe(ReleaseVersions.Version("0.19.0"));
        ask.Newest.ShouldBe(ReleaseVersions.Version("0.21.0"));
        ask.Sections.Select(section => section.Version.ToString()).ShouldBe(["0.21.0", "0.20.0"]);
    }

    [Fact]
    public void AskWithNoSectionsWhenNoNotesAreRecorded()
    {
        UpdateStatus available = new UpdateStatus.Available(ReleaseVersions.Version("0.19.0"), ReleaseVersions.Version("0.21.0"));

        UpdateDecision.Ask ask = UpdateVantage.Decide(available, ReleaseNotes.None).ShouldBeOfType<UpdateDecision.Ask>();

        ask.Newest.ShouldBe(ReleaseVersions.Version("0.21.0"));
        ask.Sections.ShouldBeEmpty();
    }

    [Fact]
    public void SayUpToDateOtherwise()
    {
        UpdateVantage.Decide(new UpdateStatus.UpToDate(), ReleaseNotes.None).ShouldBe(new UpdateDecision.Refuse(new StatusMessage.UpToDate()));
    }

    [Fact]
    public void SayTheCheckIsOff()
    {
        UpdateVantage.Decide(new UpdateStatus.Off(), ReleaseNotes.None).ShouldBe(new UpdateDecision.Refuse(new StatusMessage.UpdateCheckOff()));
    }
}