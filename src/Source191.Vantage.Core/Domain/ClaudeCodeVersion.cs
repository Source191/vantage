namespace Source191.Vantage.Domain;

/// <summary>What Vantage makes of the Claude Code version it read: nothing read, a version to show as it is, or one older than the minimum.</summary>
public abstract record ClaudeCodeVersion
{
    public sealed record Unknown : ClaudeCodeVersion;

    /// <summary>At or above the minimum, or a version Vantage cannot compare, shown as the CLI printed it.</summary>
    public sealed record Reported(string Text) : ClaudeCodeVersion;

    public sealed record BelowMinimum(ReleaseVersion Installed, ReleaseVersion Minimum) : ClaudeCodeVersion;
}