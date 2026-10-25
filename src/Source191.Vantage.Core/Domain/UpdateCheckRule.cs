namespace Source191.Vantage.Domain;

/// <summary>What Vantage knows about a newer release of itself: nothing to offer because the check is off, up to date, or one to update to.</summary>
public abstract record UpdateStatus
{
    /// <summary>The check is turned off, or this is a development build (a prerelease or an unknown version), which never checks.</summary>
    public sealed record Off : UpdateStatus;

    public sealed record UpToDate : UpdateStatus;

    public sealed record Available(ReleaseVersion Current, ReleaseVersion Newest) : UpdateStatus;
}

/// <summary>When Vantage asks nuget.org for its newest release, which answer counts, and whether it is worth offering.</summary>
public static class UpdateCheckRule
{
    public static readonly TimeSpan Interval = TimeSpan.FromHours(24);

    /// <summary>Only a release build with the check on, and only when it has never checked or last checked a day or more ago.</summary>
    public static bool ShouldCheck(ToolVersion running, bool enabled, DateTimeOffset? lastCheckedAt, DateTimeOffset now)
    {
        return enabled
            && ReleaseOf(running) is not null
            && (lastCheckedAt is null || now - lastCheckedAt.Value >= Interval);
    }

    /// <summary>The highest release in the list; prereleases and text that is not a version are skipped.</summary>
    public static ReleaseVersion? Newest(IEnumerable<string> versions)
    {
        return versions
            .Select(ReleaseVersion.TryParse)
            .OfType<ReleaseVersion>()
            .Where(version => !version.IsPrerelease)
            .Max();
    }

    /// <summary>An update is offered only to a release build with the check on, and only when the newest known release is higher.</summary>
    public static UpdateStatus Offer(ToolVersion running, bool enabled, string? newest)
    {
        ReleaseVersion? current = ReleaseOf(running);
        if (!enabled || current is null)
        {
            return new UpdateStatus.Off();
        }

        ReleaseVersion? candidate = ReleaseVersion.TryParse(newest);

        return candidate is not null && candidate.IsHigherThan(current)
            ? new UpdateStatus.Available(current, candidate)
            : new UpdateStatus.UpToDate();
    }

    private static ReleaseVersion? ReleaseOf(ToolVersion running)
    {
        ReleaseVersion? version = ReleaseVersion.TryParse(running.Text);

        return version is { IsPrerelease: false } ? version : null;
    }
}
