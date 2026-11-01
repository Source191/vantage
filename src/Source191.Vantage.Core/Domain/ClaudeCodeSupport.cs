using System.Diagnostics;

namespace Source191.Vantage.Domain;

/// <summary>The oldest Claude Code CLI Vantage supports: the version its CLI facts were verified against.</summary>
public static class ClaudeCodeSupport
{
    public static readonly ReleaseVersion Minimum = new(2, 1, 309, string.Empty);

    /// <summary>Only a version Vantage can read and compare is warned about; a false alarm is worse than none.</summary>
    public static ClaudeCodeVersion Check(ClaudeVersionReport report)
    {
        return report switch
        {
            ClaudeVersionReport.NotRead => new ClaudeCodeVersion.Unknown(),
            ClaudeVersionReport.Printed printed => Judge(printed.Text),
            _ => throw new UnreachableException(),
        };
    }

    private static ClaudeCodeVersion Judge(string reported)
    {
        return ReleaseVersion.TryParse(reported, out ReleaseVersion? installed) && Minimum.IsHigherThan(installed)
            ? new ClaudeCodeVersion.BelowMinimum(installed, Minimum)
            : new ClaudeCodeVersion.Reported(reported);
    }
}