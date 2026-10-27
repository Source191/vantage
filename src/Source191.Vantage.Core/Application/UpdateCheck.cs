using Source191.Vantage.Domain;
using Source191.Vantage.Ports;

namespace Source191.Vantage.Application;

/// <summary>
/// The newest release Vantage knows of: read from the record at start, then asked of the package registry at most once
/// a day. <see cref="ScreenLoop"/> runs <see cref="RunAsync"/> on a pool thread as the console opens; the header and the update key read
/// <see cref="Status"/>, which never waits for it. A failed request records nothing, so the next start tries again.
/// </summary>
public sealed class UpdateCheck(ToolVersion running, Func<bool> enabled, IUpdateCheckRecords records, IPackageVersions packages, TimeProvider clock)
{
    public const string PackageId = "Source191.Vantage";

    private string? newest;

    /// <summary>What the update key and the header offer now, read on every use so turning the setting off applies at once.</summary>
    public UpdateStatus Status => UpdateCheckRule.Offer(running, enabled(), Volatile.Read(ref newest));

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        UpdateCheckRecord? record = await records.ReadUpdateCheckAsync();
        Volatile.Write(ref newest, record?.Newest);
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

        string? found = UpdateCheckRule.Newest(versions)?.ToString();
        await records.WriteUpdateCheckAsync(new UpdateCheckRecord(now, found));
        Volatile.Write(ref newest, found);
    }
}