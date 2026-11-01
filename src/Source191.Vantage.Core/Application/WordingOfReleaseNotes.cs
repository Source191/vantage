using Source191.Vantage.Domain;

namespace Source191.Vantage.Application;

/// <summary>The words of the What's new dialog: its titles, buttons and lines, before an update and once after one.</summary>
public static partial class Wording
{
    private const string Bullet = "•";

    public static string UpdateNowButton => "Update now";

    public static string ReleasePageButton => "Release page";

    public static string LaterButton => "Later";

    public static string CloseButton => "Close";

    /// <summary>The dialog's title before an update: from the running version to the newest release.</summary>
    public static string WhatsNewBeforeUpdateTitle(ReleaseVersion current, ReleaseVersion newest) => $"Update Vantage {current} to {newest}";

    /// <summary>The dialog's title on the first start of a new version.</summary>
    public static string WhatsNewAfterUpdateTitle(ReleaseVersion running) => $"What's new in {running}";

    /// <summary>The dialog's lines before an update: what changed, newest release first, then what Update now does.</summary>
    public static IReadOnlyList<string> WhatsNewBeforeUpdate(IReadOnlyList<ReleaseSection> sections) =>
        [.. WhatsNewLines(sections), string.Empty, "Update now quits Vantage and updates it in a new tab; background sessions keep running."];

    /// <summary>The dialog's lines on the first start of a new version: what changed, newest release first.</summary>
    public static IReadOnlyList<string> WhatsNewAfterUpdate(IReadOnlyList<ReleaseSection> sections) => WhatsNewLines(sections);

    /// <summary>
    /// Each release's version, then its entries under their headings in the changelog's order, each ending with its pull
    /// request number as written; a blank line between releases. No sections means the notes could not be read.
    /// </summary>
    private static IReadOnlyList<string> WhatsNewLines(IReadOnlyList<ReleaseSection> sections)
    {
        ArgumentNullException.ThrowIfNull(sections);
        if (sections.Count == 0)
        {
            return ["The notes for this release could not be read; Release page shows them on GitHub."];
        }

        return [.. sections.SelectMany((section, index) => index == 0 ? SectionLines(section) : [string.Empty, .. SectionLines(section)])];
    }

    private static IEnumerable<string> SectionLines(ReleaseSection section)
    {
        yield return section.Version.ToString();
        if (section.Entries.Count == 0)
        {
            yield return "  No changes you would notice.";
            yield break;
        }

        foreach (IGrouping<ChangeKind, ReleaseEntry> kind in section.Entries.GroupBy(entry => entry.Kind).OrderBy(group => group.Key))
        {
            yield return $"  {ChangeHeading(kind.Key)}";
            foreach (ReleaseEntry entry in kind)
            {
                yield return $"    {Bullet} {entry.Text}";
            }
        }
    }

    private static string ChangeHeading(ChangeKind kind) => kind switch
    {
        ChangeKind.Added => "Added",
        ChangeKind.Changed => "Changed",
        ChangeKind.Fixed => "Fixed",
        ChangeKind.Removed => "Removed",
        ChangeKind.Security => "Security",
        ChangeKind.Internal => "Internal",
    };
}