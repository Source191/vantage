using System.Diagnostics.CodeAnalysis;

namespace Source191.Vantage.Domain;

/// <summary>
/// The release notes Vantage shows, read from the changelog as it travels in the package (its nuspec release notes on
/// nuget.org, and the file beside the installed tool): a narrow
/// <see href="https://keepachangelog.com/">Keep a Changelog</see> of <c>## 0.21.0</c> sections (a <c> - date</c> after
/// the version is allowed), <c>### Added</c> and the other <see cref="ChangeKind"/> headings, and one-line <c>- </c>
/// entries. It is text from outside Vantage, so <see cref="TryParse"/> drops every control character as it reads each
/// line and skips whatever it does not understand, including the Unreleased section; <see cref="None"/> stands for
/// notes that were never read.
/// </summary>
public sealed record ReleaseNotes
{
    private const string SectionMark = "## ";
    private const string HeadingMark = "### ";
    private const string EntryMark = "- ";

    /// <summary>No notes: none were recorded, or they could not be read.</summary>
    public static readonly ReleaseNotes None = new(string.Empty, []);

    private ReleaseNotes(string text, IReadOnlyList<ReleaseSection> sections)
    {
        Text = text;
        Sections = sections;
    }

    /// <summary>The text as it was read, which the update-check record keeps so it is parsed again when read back.</summary>
    public string Text { get; }

    /// <summary>Every version section in the order written, prereleases and Internal entries included.</summary>
    public IReadOnlyList<ReleaseSection> Sections { get; }

    /// <summary>Reads changelog text; false when it holds no version section, as the link-only notes of earlier packages do.</summary>
    public static bool TryParse(string? text, [MaybeNullWhen(false)] out ReleaseNotes notes)
    {
        List<ReleaseSection> sections = [];
        SectionReader reader = new(sections);
        foreach (string line in (text ?? string.Empty).Split('\n'))
        {
            reader.Read(Visible(line).Trim());
        }

        reader.Close();
        notes = sections.Count > 0 ? new ReleaseNotes(text ?? string.Empty, sections) : null;

        return notes is not null;
    }

    /// <summary>The released (never prerelease) sections after <paramref name="after"/> up to and including <paramref name="upTo"/>, newest first, without their Internal entries.</summary>
    public IReadOnlyList<ReleaseSection> Between(ReleaseVersion after, ReleaseVersion upTo)
    {
        ArgumentNullException.ThrowIfNull(after);
        ArgumentNullException.ThrowIfNull(upTo);

        return [.. Sections
            .Where(section => !section.Version.IsPrerelease && section.Version > after && section.Version <= upTo)
            .OrderByDescending(section => section.Version)
            .Select(section => section with { Entries = [.. section.Entries.Where(entry => entry.Kind != ChangeKind.Internal)] })];
    }

    /// <summary>The line without control characters (Unicode category Cc, which holds ESC, BEL and the carriage return).</summary>
    private static string Visible(string line) => string.Concat(line.Where(character => !char.IsControl(character)));

    /// <summary>Follows the headings line by line and collects the entries of each version section.</summary>
    private sealed class SectionReader(List<ReleaseSection> sections)
    {
        private readonly List<ReleaseSection> sections = sections;
        private readonly List<ReleaseEntry> entries = [];
        private ReleaseVersion? version;
        private ChangeKind? kind;

        public void Read(string line)
        {
            if (line.StartsWith(SectionMark, StringComparison.Ordinal))
            {
                StartSection(line[SectionMark.Length..]);
                return;
            }

            if (line.StartsWith(HeadingMark, StringComparison.Ordinal))
            {
                kind = KindOf(line[HeadingMark.Length..].Trim());
                return;
            }

            if (line.StartsWith(EntryMark, StringComparison.Ordinal) && version is not null && kind is ChangeKind current)
            {
                entries.Add(new ReleaseEntry(current, line[EntryMark.Length..].Trim()));
            }
        }

        public void Close()
        {
            if (version is not null)
            {
                sections.Add(new ReleaseSection(version, [.. entries]));
            }

            entries.Clear();
        }

        /// <summary>A heading's first word is its version (<c>0.21.0 - 2026-11-20</c>); anything else, such as Unreleased, starts a section whose entries are skipped.</summary>
        private void StartSection(string heading)
        {
            Close();
            version = ReleaseVersion.TryParse(heading.Trim().Split(' ')[0], out ReleaseVersion? read) ? read : null;
            kind = null;
        }

        /// <summary>One of the names exactly; a number or another word is not a heading Vantage knows.</summary>
        private static ChangeKind? KindOf(string name) => Enum.GetNames<ChangeKind>().Contains(name, StringComparer.Ordinal) ? Enum.Parse<ChangeKind>(name) : null;
    }
}
