using Source191.Vantage.Application;
using Source191.Vantage.Domain;

namespace Source191.Vantage.Core.Tests.Application;

public sealed class WordingOfReleaseNotesShould
{
    private const string Changelog = "## 0.21.0\n### Fixed\n- Renaming an order keeps its history (#169)\n### Added\n- A key that opens the Northwind order (#171)\n### Internal\n- The order files are split (#170)\n## 0.20.0\n### Changed\n- The order list is sorted by date (#160)\n## 0.19.5\n### Internal\n- Only internal work (#155)\n";

    [Fact]
    public void TitleTheDialogBeforeAnUpdateFromTheRunningToTheNewestVersion()
    {
        Wording.WhatsNewBeforeUpdateTitle(ReleaseVersions.Version("0.19.0"), ReleaseVersions.Version("0.21.0")).ShouldBe("Update Vantage 0.19.0 to 0.21.0");
    }

    [Fact]
    public void TitleTheDialogAfterAnUpdateWithTheRunningVersion()
    {
        Wording.WhatsNewAfterUpdateTitle(ReleaseVersions.Version("0.21.0")).ShouldBe("What's new in 0.21.0");
    }

    [Fact]
    public void NameTheButtons()
    {
        Wording.UpdateNowButton.ShouldBe("Update now");
        Wording.ReleasePageButton.ShouldBe("Release page");
        Wording.LaterButton.ShouldBe("Later");
        Wording.CloseButton.ShouldBe("Close");
    }

    [Fact]
    public void ListEachReleaseUnderItsVersionWithEntriesUnderTheirHeadings()
    {
        IReadOnlyList<ReleaseSection> sections = Sections(ReleaseVersions.Version("0.19.0"), ReleaseVersions.Version("0.21.0"));

        Wording.WhatsNewAfterUpdate(sections).ShouldBe(
        [
            "0.21.0",
            "  Added",
            "    • A key that opens the Northwind order (#171)",
            "  Fixed",
            "    • Renaming an order keeps its history (#169)",
            string.Empty,
            "0.20.0",
            "  Changed",
            "    • The order list is sorted by date (#160)",
            string.Empty,
            "0.19.5",
            "  No changes you would notice.",
        ]);
    }

    [Fact]
    public void SayWhatUpdateNowDoesBeforeAnUpdate()
    {
        IReadOnlyList<ReleaseSection> sections = Sections(ReleaseVersions.Version("0.20.0"), ReleaseVersions.Version("0.21.0"));

        IReadOnlyList<string> lines = Wording.WhatsNewBeforeUpdate(sections);

        lines[0].ShouldBe("0.21.0");
        lines[^1].ShouldBe("Update now quits Vantage and updates it in a new tab; background sessions keep running.");
    }

    [Fact]
    public void SayTheNotesCouldNotBeRead()
    {
        Wording.WhatsNewBeforeUpdate([])[0].ShouldBe("The notes for this release could not be read; Release page shows them on GitHub.");
    }

    [Fact]
    public void EndAnEntryLineWithItsPullRequestNumber()
    {
        IReadOnlyList<string> lines = Wording.WhatsNewAfterUpdate(Sections(ReleaseVersions.Version("0.20.0"), ReleaseVersions.Version("0.21.0")));

        lines.Where(line => line.TrimStart().StartsWith('•')).ShouldAllBe(line => line.EndsWith(')') && line.Contains("(#", StringComparison.Ordinal));
    }

    private static IReadOnlyList<ReleaseSection> Sections(ReleaseVersion after, ReleaseVersion upTo)
    {
        ReleaseNotes.TryParse(Changelog, out ReleaseNotes? notes).ShouldBeTrue();

        return notes.Between(after, upTo);
    }
}