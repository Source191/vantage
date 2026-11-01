using Source191.Vantage.Domain;

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
                .Select(child => new MemoryShare(ProgramLabel.From(child.Process.Name, child.Usage is ProcessMeasure.Measured measured ? measured.Usage.CommandLine : string.Empty), child.Total))
                .OrderByDescending(share => share.Memory),
        ];

        return new MemoryBreakdown(sessionName, tree.Usage is ProcessMeasure.Measured root ? root.Usage.PrivateMemory : MemorySize.Zero, programs, tree.Total, tree.Count);
    }
}