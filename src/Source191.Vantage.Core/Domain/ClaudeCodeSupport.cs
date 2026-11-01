using System.Diagnostics;

namespace Source191.Vantage.Domain;

/// <summary>
/// The oldest Claude Code CLI Vantage supports. The CLI facts Vantage relies on were verified on a later version, which
/// <c>.claude\rules\claude-code.md</c> records with its date.
/// </summary>
public static class ClaudeCodeSupport
{
    public static readonly ReleaseVersion Minimum = ReleaseVersion.From("2.1.309");

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