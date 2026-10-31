using Source191.Vantage.Domain;
using Source191.Vantage.Ports;
using System.Diagnostics;

namespace Source191.Vantage.Application;

/// <summary>
/// The newest release Vantage knows of: read from the record at start, then asked of the package registry at most once
/// a day. <see cref="ScreenLoop"/> runs <see cref="RunAsync"/> on a pool thread as the console opens; the header and the update key read
/// <see cref="Status"/>, which never waits for it. A failed request records nothing, so the next start tries again.
/// </summary>
public sealed class UpdateCheck(ToolVersion running, Func<bool> enabled, IUpdateCheckRecords records, IPackageVersions packages, TimeProvider clock)
{
    public const string PackageId = "Source191.Vantage";

    /// <summary>The feed the check consults, and the only one the update tab installs from, whatever else the machine has configured.</summary>
    public const string Source = "https://api.nuget.org/v3/index.json";

    private readonly SharedSnapshot<NewestRelease> newest = new(new NewestRelease.None());

    /// <summary>What the update key and the header offer now, read on every use so turning the setting off applies at once.</summary>
    public UpdateStatus Status => UpdateCheckRule.Offer(running, enabled(), newest.Value);

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        UpdateCheckRecord? record = await records.ReadUpdateCheckAsync();
        newest.Replace(record?.Newest is string text ? NewestRelease.Recorded(text) : new NewestRelease.None());
        DateTimeOffset now = clock.GetUtcNow();
        if (!UpdateCheckRule.ShouldCheck(running, enabled(), record?.CheckedAt, now))
        {
            return;
        }

        IReadOnlyList<string>? versions = await packages.ListAsync(PackageId, cancellationToken);
        if (versions is null)
        {
            return;
        }

        NewestRelease found = UpdateCheckRule.Newest(versions);
        await records.WriteUpdateCheckAsync(new UpdateCheckRecord(now, RecordedText(found)));
        newest.Replace(found);
    }

    /// <summary>The record keeps the newest release as text, and no text when there is none.</summary>
    private static string? RecordedText(NewestRelease release)
    {
        return release switch
        {
            NewestRelease.Found found => found.Version.ToString(),
            NewestRelease.None => null,
            _ => throw new UnreachableException(),
        };
    }
}