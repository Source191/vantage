using Source191.Vantage.Domain;

namespace Source191.Vantage.Core.Tests.Domain;

public sealed class ReleaseNotesShould
{
    private const string Changelog = """
        # Changelog

        Written for the people who use Northwind Tools.

        ## Unreleased

        ### Added

        - A key that is not released yet (#175)

        ## 0.21.0 - 2026-11-20

        ### Added

        - A key that opens the Northwind order in the browser (#171)

        ### Fixed

        - Renaming an order keeps its history (#169)

        ### Internal

        - The order files are split one type per file (#170)

        ## 0.21.0-preview.2

        ### Added

        - A preview of the order key (#168)

        ## 0.20.0

        ### Changed

        - The order list is sorted by date (#160)

        ## 0.19.0

        ### Fixed

        - The supplier column no longer flickers (#150)
        """;

    [Fact]
    public void ReadTheSectionsOfAChangelog()
    {
        ReleaseNotes.TryParse(Changelog, out ReleaseNotes? notes).ShouldBeTrue();

        notes.Sections.Select(section => section.Version.ToString()).ShouldBe(["0.21.0", "0.21.0-preview.2", "0.20.0", "0.19.0"]);
        notes.Sections[0].Entries.ShouldBe(
        [
            new ReleaseEntry(ChangeKind.Added, "A key that opens the Northwind order in the browser (#171)"),
            new ReleaseEntry(ChangeKind.Fixed, "Renaming an order keeps its history (#169)"),
            new ReleaseEntry(ChangeKind.Internal, "The order files are split one type per file (#170)"),
        ]);
        notes.Sections[2].Entries.ShouldBe([new ReleaseEntry(ChangeKind.Changed, "The order list is sorted by date (#160)")]);
        notes.Text.ShouldBe(Changelog);
    }

    [Fact]
    public void ReadAChangelogWithWindowsLineEndings()
    {
        ReleaseNotes.TryParse(Changelog.ReplaceLineEndings("\r\n"), out ReleaseNotes? notes).ShouldBeTrue();

        notes.Sections[0].Entries[1].ShouldBe(new ReleaseEntry(ChangeKind.Fixed, "Renaming an order keeps its history (#169)"));
    }

    [Theory]
    [InlineData("Release notes: https://github.com/Source191/vantage/releases/tag/v0.16.1")]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("## Unreleased\n\n### Added\n\n- A key that is not released yet (#175)")]
    public void AnswerFalseForTextWithNoVersionSection(string? text)
    {
        ReleaseNotes.TryParse(text, out _).ShouldBeFalse();
    }

    [Fact]
    public void HaveNoSectionsWhenThereAreNone()
    {
        ReleaseNotes.None.Sections.ShouldBeEmpty();
        ReleaseNotes.None.Text.ShouldBeEmpty();
        ReleaseNotes.None.Between(ReleaseVersions.Version("0.19.0"), ReleaseVersions.Version("0.21.0")).ShouldBeEmpty();
    }

    [Fact]
    public void ChooseTheStableSectionsBetweenTwoVersionsNewestFirst()
    {
        ReleaseNotes notes = Parsed(Changelog);

        IReadOnlyList<ReleaseSection> between = notes.Between(ReleaseVersions.Version("0.19.0"), ReleaseVersions.Version("0.21.0"));

        between.Select(section => section.Version.ToString()).ShouldBe(["0.21.0", "0.20.0"]);
    }

    [Fact]
    public void PutTheNewestSectionFirstWhateverTheOrderInTheFile()
    {
        ReleaseNotes notes = Parsed("## 0.20.0\n### Fixed\n- Older (#1)\n## 0.21.0\n### Fixed\n- Newer (#2)");

        notes.Between(ReleaseVersions.Version("0.19.0"), ReleaseVersions.Version("0.21.0")).Select(section => section.Version.ToString()).ShouldBe(["0.21.0", "0.20.0"]);
    }

    [Fact]
    public void LeaveOutSectionsAboveTheVersionUpdatedTo()
    {
        ReleaseNotes notes = Parsed(Changelog);

        notes.Between(ReleaseVersions.Version("0.19.0"), ReleaseVersions.Version("0.20.0")).Select(section => section.Version.ToString()).ShouldBe(["0.20.0"]);
    }

    [Fact]
    public void LeaveOutInternalEntries()
    {
        ReleaseNotes notes = Parsed(Changelog);

        ReleaseSection newest = notes.Between(ReleaseVersions.Version("0.20.0"), ReleaseVersions.Version("0.21.0")).Single();

        newest.Entries.Select(entry => entry.Kind).ShouldBe([ChangeKind.Added, ChangeKind.Fixed]);
    }

    [Fact]
    public void DropControlCharactersFromEachLine()
    {
        ReleaseNotes notes = Parsed("## 0.21.0\n### Fixed\n- The title \u001b]2;owned\u0007stays put (#171)\n");

        notes.Sections.Single().Entries.Single().Text.ShouldBe("The title ]2;ownedstays put (#171)");
    }

    [Fact]
    public void SkipLinesItDoesNotUnderstand()
    {
        const string text = """
            ## 0.21.0
            Some prose about the release.
            - An entry before any heading (#1)
            ### Added
            - A real entry (#171)
            | Column | Value |
            |--------|-------|
            ### Thanks
            - Under a heading of an unknown kind (#2)
            ### 3
            - Under a heading that is a number (#3)
            ## Not a version
            ### Added
            - Under a section that is not a version (#4)
            """;

        ReleaseNotes notes = Parsed(text);

        notes.Sections.Single().Entries.ShouldBe([new ReleaseEntry(ChangeKind.Added, "A real entry (#171)")]);
    }

    private static ReleaseNotes Parsed(string text)
    {
        ReleaseNotes.TryParse(text, out ReleaseNotes? notes).ShouldBeTrue();

        return notes;
    }
}