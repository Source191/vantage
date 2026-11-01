using Source191.Vantage.Domain;
using System.Diagnostics;

namespace Source191.Vantage.Application;

/// <summary>One program a session started, with everything under it: an MCP server, a shell, whatever else.</summary>
public sealed record MemoryShare(string Label, MemorySize Memory);

/// <summary>
/// What a session's memory is made of, for the breakdown dialog: Claude Code's own process, then each program it
/// started directly with that program's whole subtree, largest first, then the total and the process count.
/// </summary>
public sealed record MemoryBreakdown(string SessionName, MemorySize ClaudeCode, IReadOnlyList<MemoryShare> Programs, MemorySize Total, int ProcessCount)
{
    public static MemoryBreakdown Of(string sessionName, ProcessTree tree)
    {
        ArgumentNullException.ThrowIfNull(tree);

        IReadOnlyList<MemoryShare> programs =
        [
            .. tree.Children
                .Select(child => new MemoryShare(ProgramLabel.From(child.Process.Name, CommandLineOf(child.Usage)), child.Total))
                .OrderByDescending(share => share.Memory),
        ];

        return new MemoryBreakdown(sessionName, tree.Own, programs, tree.Total, tree.Count);
    }

    /// <summary>The command line a program is labelled from; empty for one that refused to be measured, so it is labelled by its name alone.</summary>
    private static string CommandLineOf(ProcessMeasure measure) => measure switch
    {
        ProcessMeasure.Measured measured => measured.Usage.CommandLine,
        ProcessMeasure.Unmeasured => string.Empty,
        _ => throw new UnreachableException(),
    };
}