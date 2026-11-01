using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;
using Source191.Vantage.Ports;
using System.Diagnostics;

namespace Source191.Vantage.Application;

/// <summary>
/// The newest release Vantage knows of and its notes: read from the record at start, then asked of the package registry
/// at most once a day, the notes only when that release is newer than the running one and none are recorded for it.
/// <see cref="ScreenLoop"/> runs <see cref="RunAsync"/> on a pool thread as the console opens; the settings row, the
/// update key and the screen read <see cref="Status"/>, <see cref="Notes"/> and <see cref="TakeAnnouncement"/>, which
/// never wait for it. A failed request records nothing, so the next start tries again.
/// </summary>
public sealed class UpdateCheck(ToolVersion running, Func<bool> enabled, IUpdateCheckRecords records, IPackageVersions packages, TimeProvider clock)
{
    public const string PackageId = "Source191.Vantage";

    /// <summary>The feed the check consults, and the only one the update tab installs from, whatever else the machine has configured.</summary>
    public const string Source = "https://api.nuget.org/v3/index.json";

    private readonly SharedSnapshot<KnownRelease> known = new(new KnownRelease(new NewestRelease.None(), ReleaseNotes.None));
    private readonly SharedSnapshot<StatusMessage> announcement = new(StatusMessage.None);

    /// <summary>What the update key and the settings row offer now, read on every use so turning the setting off applies at once.</summary>
    public UpdateStatus Status => UpdateCheckRule.Offer(running, enabled(), known.Value.Newest);

    /// <summary>The newest release's notes, or <see cref="ReleaseNotes.None"/> when none were read.</summary>
    public ReleaseNotes Notes => known.Value.Notes;

    /// <summary>
    /// Once, after a check found a release newer than both the running version and the one recorded before:
    /// <see cref="StatusMessage.NewerReleaseFound"/>. Taking it clears it, so every later call answers
    /// <see cref="StatusMessage.None"/>; only the UI thread takes it.
    /// </summary>
    public StatusMessage TakeAnnouncement()
    {
        StatusMessage taken = announcement.Value;
        announcement.Replace(StatusMessage.None);

        return taken;
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        DateTimeOffset now = clock.GetUtcNow();
        (KnownRelease recorded, bool due) = await records.ReadUpdateCheckAsync() switch
        {
            UpdateCheckRecord.Checked last => (new KnownRelease(last.Newest, last.NewestNotes), UpdateCheckRule.ShouldCheck(running, enabled(), last.CheckedAt, now)),
            UpdateCheckRecord.Never => (new KnownRelease(new NewestRelease.None(), ReleaseNotes.None), UpdateCheckRule.ShouldCheck(running, enabled())),
            _ => throw new UnreachableException(),
        };
        known.Replace(recorded);
        if (!due)
        {
            return;
        }

        if (await packages.ListAsync(PackageId, cancellationToken) is not VersionListing.Listed listed)
        {
            return;
        }

        NewestRelease found = UpdateCheckRule.Newest(listed.Versions);
        ReleaseNotes notes = await NotesOfAsync(found, recorded, cancellationToken);
        await records.WriteUpdateCheckAsync(new UpdateCheckRecord.Checked(now, found, notes));
        known.Replace(new KnownRelease(found, notes));
        if (UpdateCheckRule.Offer(running, enabled(), found) is UpdateStatus.Available available && IsNewerThan(available.Newest, recorded.Newest))
        {
            announcement.Replace(new StatusMessage.NewerReleaseFound(available.Newest));
        }
    }

    /// <summary>The notes of a release worth offering: those already recorded for it, or else asked for once; none for a release that is not newer.</summary>
    private async Task<ReleaseNotes> NotesOfAsync(NewestRelease found, KnownRelease recorded, CancellationToken cancellationToken)
    {
        if (UpdateCheckRule.Offer(running, enabled(), found) is not UpdateStatus.Available available)
        {
            return ReleaseNotes.None;
        }

        if (recorded.Newest == found && recorded.Notes != ReleaseNotes.None)
        {
            return recorded.Notes;
        }

        PackageNotes read = await packages.NotesAsync(PackageId, available.Newest, cancellationToken);

        return read is PackageNotes.Read text && ReleaseNotes.TryParse(text.Text, out ReleaseNotes? notes) ? notes : ReleaseNotes.None;
    }

    private static bool IsNewerThan(ReleaseVersion version, NewestRelease recorded) => recorded is not NewestRelease.Found before || version.IsHigherThan(before.Version);

    /// <summary>The newest release and its notes, replaced together so a reader never sees one without the other.</summary>
    private sealed record KnownRelease(NewestRelease Newest, ReleaseNotes Notes);
}