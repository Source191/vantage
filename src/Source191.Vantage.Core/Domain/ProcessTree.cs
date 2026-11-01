using System.Diagnostics;

namespace Source191.Vantage.Domain;

/// <summary>
/// A process and everything it started, as the table listed them, each with what it uses. The root must be measurable;
/// a member that refuses counts as nothing and keeps its children.
/// </summary>
public sealed record ProcessTree(ProcessFacts Process, ProcessMeasure Usage, IReadOnlyList<ProcessTree> Children)
{
    /// <summary>The tree under a root, or <see cref="ProcessTreeLookup.NotMeasurable"/> when the root is not in the table or cannot be measured.</summary>
    public static ProcessTreeLookup Under(int rootId, IReadOnlyList<ProcessFacts> snapshot, Func<int, ProcessUsage?> usageOf)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(usageOf);

        ProcessFacts? root = snapshot.FirstOrDefault(process => process.ProcessId == rootId);
        if (root is null || usageOf(rootId) is not { } usage)
        {
            return new ProcessTreeLookup.NotMeasurable();
        }

        ILookup<int, ProcessFacts> byParent = snapshot.ToLookup(process => process.ParentId);

        return new ProcessTreeLookup.Found(Build(root, new ProcessMeasure.Measured(usage), usage.StartedAt, byParent, usageOf));
    }

    public MemorySize Total => Children.Aggregate(Own, (sum, child) => sum + child.Total);

    public int Count => 1 + Children.Sum(child => child.Count);

    /// <summary>
    /// Windows reuses identifiers, so a process whose recorded parent is this one may be older than it: an orphan of
    /// the identifier's previous owner. A child is kept only when it started after the nearest measured ancestor.
    /// </summary>
    private static ProcessTree Build(ProcessFacts process, ProcessMeasure measure, DateTimeOffset ancestorStartedAt, ILookup<int, ProcessFacts> byParent, Func<int, ProcessUsage?> usageOf)
    {
        List<ProcessTree> children = [];
        foreach (ProcessFacts child in byParent[process.ProcessId])
        {
            ProcessMeasure childMeasure = usageOf(child.ProcessId) is { } usage ? new ProcessMeasure.Measured(usage) : new ProcessMeasure.Unmeasured();
            DateTimeOffset childStartedAt = StartedAt(childMeasure, ancestorStartedAt);
            if (childStartedAt < ancestorStartedAt)
            {
                continue;
            }

            children.Add(Build(child, childMeasure, childStartedAt, byParent, usageOf));
        }

        return new ProcessTree(process, measure, children);
    }

    /// <summary>When a measured member started; an unmeasured one stands at its nearest measured ancestor's start.</summary>
    private static DateTimeOffset StartedAt(ProcessMeasure measure, DateTimeOffset ancestorStartedAt) => measure switch
    {
        ProcessMeasure.Measured measured => measured.Usage.StartedAt,
        ProcessMeasure.Unmeasured => ancestorStartedAt,
        _ => throw new UnreachableException(),
    };

    private MemorySize Own => Usage switch
    {
        ProcessMeasure.Measured measured => measured.Usage.PrivateMemory,
        ProcessMeasure.Unmeasured => MemorySize.Zero,
        _ => throw new UnreachableException(),
    };
}

/// <summary>A process tree looked up by its root: the tree, or the reason there is none to measure.</summary>
public abstract record ProcessTreeLookup
{
    public sealed record Found(ProcessTree Tree) : ProcessTreeLookup;

    /// <summary>The root is not in the process table, or it refused to be measured.</summary>
    public sealed record NotMeasurable : ProcessTreeLookup;
}