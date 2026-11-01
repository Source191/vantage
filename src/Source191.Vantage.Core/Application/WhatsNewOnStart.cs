using Source191.Vantage.Application.Diagnostics;
using Source191.Vantage.Domain;
using Source191.Vantage.Ports;

namespace Source191.Vantage.Application;

/// <summary>
/// Whether the first start of a new version says what changed: only a released (never a prerelease) version higher
/// than the released version that last ran, from the notes inside the installed tool. The running version is recorded
/// whenever it differs from the one recorded, so the dialog shows once, a first ever start shows nothing, and notes
/// that cannot be read show nothing and are logged without their text. A development build neither reads nor records.
/// </summary>
public sealed class WhatsNewOnStart(ToolVersion running, ILastRunVersion lastRun, IPackagedReleaseNotes packaged, DiagnosticLog log)
{
    private readonly ToolVersion running = running ?? throw new ArgumentNullException(nameof(running));
    private readonly ILastRunVersion lastRun = lastRun ?? throw new ArgumentNullException(nameof(lastRun));
    private readonly IPackagedReleaseNotes packaged = packaged ?? throw new ArgumentNullException(nameof(packaged));
    private readonly DiagnosticLog log = log ?? throw new ArgumentNullException(nameof(log));

    public async Task<WhatsNewAtStart> DecideAsync()
    {
        if (!ReleaseVersion.TryParse(running.Text, out ReleaseVersion? current) || current.IsPrerelease)
        {
            return WhatsNewAtStart.Nothing;
        }

        LastRun last = await lastRun.ReadLastRunAsync();
        if (last is LastRun.Ran same && same.Version == current)
        {
            return WhatsNewAtStart.Nothing;
        }

        await lastRun.WriteLastRunAsync(current);

        return last is LastRun.Ran earlier && current.IsHigherThan(earlier.Version) ? await ChangesSinceAsync(earlier.Version, current) : WhatsNewAtStart.Nothing;
    }

    private async Task<WhatsNewAtStart> ChangesSinceAsync(ReleaseVersion earlier, ReleaseVersion current)
    {
        if (await packaged.ReadAsync() is not PackageNotes.Read read || !ReleaseNotes.TryParse(read.Text, out ReleaseNotes? notes))
        {
            log.Record(new PackagedNotesUnreadable(running));
            return WhatsNewAtStart.Nothing;
        }

        IReadOnlyList<ReleaseSection> sections = notes.Between(earlier, current);

        return sections.Count == 0 ? WhatsNewAtStart.Nothing : new WhatsNewAtStart.Show(current, sections);
    }
}