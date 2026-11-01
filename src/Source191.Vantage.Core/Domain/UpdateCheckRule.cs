namespace Source191.Vantage.Domain;

/// <summary>When Vantage asks nuget.org for its newest release, which answer counts, and whether it is worth offering.</summary>
public static class UpdateCheckRule
{
    public static readonly TimeSpan Interval = TimeSpan.FromHours(24);

    /// <summary>With no previous check: only a release build with the check on, and straight away.</summary>
    public static bool ShouldCheck(ToolVersion running, bool enabled) => enabled && ReleaseOf(running) is not null;

    /// <summary>Only a release build with the check on, and only when it last checked a day or more ago.</summary>
    public static bool ShouldCheck(ToolVersion running, bool enabled, DateTimeOffset lastCheckedAt, DateTimeOffset now) => ShouldCheck(running, enabled) && now - lastCheckedAt >= Interval;

    /// <summary>The highest release in the list, or none; prereleases and text that is not a version are skipped.</summary>
    public static NewestRelease Newest(IEnumerable<string> versions)
    {
        ReleaseVersion? highest = ReleaseVersion.Parsed(versions)
            .Where(version => !version.IsPrerelease)
            .Max();

        return highest is null ? new NewestRelease.None() : new NewestRelease.Found(highest);
    }

    /// <summary>An update is offered only to a release build with the check on, and only when the newest known release is higher.</summary>
    public static UpdateStatus Offer(ToolVersion running, bool enabled, NewestRelease newest)
    {
        ReleaseVersion? current = ReleaseOf(running);
        if (!enabled || current is null)
        {
            return new UpdateStatus.Off();
        }

        return newest is NewestRelease.Found found && found.Version.IsHigherThan(current)
            ? new UpdateStatus.Available(current, found.Version)
            : new UpdateStatus.UpToDate();
    }

    private static ReleaseVersion? ReleaseOf(ToolVersion running) => ReleaseVersion.TryParse(running.Text, out ReleaseVersion? version) && !version.IsPrerelease ? version : null;
}