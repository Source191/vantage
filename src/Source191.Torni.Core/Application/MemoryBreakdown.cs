using Source191.Torni.Domain;
using System.Diagnostics;

namespace Source191.Torni.Application;

/// <summary>
/// What a session's memory is made of, for the breakdown dialog: Claude Code's own process, then each program it
/// started directly with that program's whole subtree, largest first, then the total and the process count.
/// </summary>
public sealed record MemoryBreakdown(ListedName SessionName, MemorySize ClaudeCode, IReadOnlyList<MemoryShare> Programs, MemorySize Total, int ProcessCount)
{
    public static MemoryBreakdown Of(ListedName sessionName, ProcessTree tree)
    {
        IReadOnlyList<MemoryShare> programs =
        [
            .. tree.Children
                .Select(child => new MemoryShare(LabelOf(child), child.Total))
                .OrderByDescending(share => share.Memory),
        ];

        return new MemoryBreakdown(sessionName, tree.Own, programs, tree.Total, tree.Count);
    }

    /// <summary>The label the adapter made from a measured program's command line; a program that refused to be measured, or whose label is empty, is labelled by its name alone.</summary>
    private static string LabelOf(ProcessTree child) => child.Usage switch
    {
        ProcessMeasure.Measured { Usage.Label.Length: > 0 } measured => measured.Usage.Label,
        ProcessMeasure.Measured => ProgramLabel.From(child.Process.Name, string.Empty),
        ProcessMeasure.Unmeasured => ProgramLabel.From(child.Process.Name, string.Empty),
        _ => throw new UnreachableException(),
    };
}