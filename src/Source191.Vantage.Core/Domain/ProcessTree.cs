namespace Source191.Vantage.Domain;

/// <summary>
/// A process and everything it started, as the table listed them, each with what it uses. The root must be measurable;
/// a member that refuses counts as nothing and keeps its children.
/// </summary>
public sealed record ProcessTree(ProcessFacts Process, ProcessUsage? Usage, IReadOnlyList<ProcessTree> Children)
{
    /// <summary>The tree under a root, or null when the root is not in the table or cannot be measured.</summary>
    public static ProcessTree? Under(int rootId, IReadOnlyList<ProcessFacts> snapshot, Func<int, ProcessUsage?> usageOf)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(usageOf);

        ProcessFacts? root = snapshot.FirstOrDefault(process => process.ProcessId == rootId);
        ProcessUsage? usage = root is null ? null : usageOf(rootId);
        if (root is null || usage is null)
        {
            return null;
        }

        ILookup<int, ProcessFacts> byParent = snapshot.ToLookup(process => process.ParentId);

        return Build(root, usage, usage.StartedAt, byParent, usageOf);
    }

    public MemorySize Total => Children.Aggregate(Usage?.PrivateMemory ?? MemorySize.Zero, (sum, child) => sum + child.Total);

    public int Count => 1 + Children.Sum(child => child.Count);

    /// <summary>
    /// Windows reuses identifiers, so a process whose recorded parent is this one may be older than it: an orphan of
    /// the identifier's previous owner. A child is kept only when it started after the nearest measured ancestor.
    /// </summary>
    private static ProcessTree Build(ProcessFacts process, ProcessUsage? usage, DateTimeOffset ancestorStartedAt, ILookup<int, ProcessFacts> byParent, Func<int, ProcessUsage?> usageOf)
    {
        List<ProcessTree> children = [];
        foreach (ProcessFacts child in byParent[process.ProcessId])
        {
            ProcessUsage? childUsage = usageOf(child.ProcessId);
            if (childUsage is not null && childUsage.StartedAt < ancestorStartedAt)
            {
                continue;
            }

            children.Add(Build(child, childUsage, childUsage?.StartedAt ?? ancestorStartedAt, byParent, usageOf));
        }

        return new ProcessTree(process, usage, children);
    }
}