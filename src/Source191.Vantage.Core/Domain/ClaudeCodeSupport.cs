namespace Source191.Vantage.Domain;

/// <summary>What Vantage makes of the Claude Code version it read: nothing read, a version to show as it is, or one older than the minimum.</summary>
public abstract record ClaudeCodeVersion
{
    public sealed record Unknown : ClaudeCodeVersion;

    /// <summary>At or above the minimum, or a version Vantage cannot compare, shown as the CLI printed it.</summary>
    public sealed record Reported(string Text) : ClaudeCodeVersion;

    public sealed record BelowMinimum(ReleaseVersion Installed, ReleaseVersion Minimum) : ClaudeCodeVersion;
}

/// <summary>The oldest Claude Code CLI Vantage supports: the version its CLI facts were verified against.</summary>
public static class ClaudeCodeSupport
{
    public static readonly ReleaseVersion Minimum = new(2, 1, 309, null);

    /// <summary>Only a version Vantage can read and compare is warned about; a false alarm is worse than none.</summary>
    public static ClaudeCodeVersion Check(string? reported)
    {
        if (reported is null)
        {
            return new ClaudeCodeVersion.Unknown();
        }

        return ReleaseVersion.TryParse(reported) is ReleaseVersion installed && Minimum.IsHigherThan(installed)
            ? new ClaudeCodeVersion.BelowMinimum(installed, Minimum)
            : new ClaudeCodeVersion.Reported(reported);
    }
}