using Source191.Torni.Domain;

namespace Source191.Torni.TestDoubles;

/// <summary>
/// A stand-in for the operating system's process table: the only boundary the tab registry and the session actions touch.
/// </summary>
public sealed class FakeProcessRegistry : IProcessRegistry
{
    private readonly Dictionary<int, DateTimeOffset> running = [];
    private readonly Dictionary<int, (int ParentId, string Name)> tree = [];
    private readonly Dictionary<int, ProcessUsage> usage = [];

    public FakeProcessRegistry Running(int processId, DateTimeOffset startedAt)
    {
        running[processId] = startedAt;
        return this;
    }

    /// <summary>Declares a process, its name and its parent, so a tab's shell can be found by walking up and a tree by walking down.</summary>
    public FakeProcessRegistry InTree(int processId, string name, int parentId)
    {
        tree[processId] = (parentId, name);
        return this;
    }

    /// <summary>Declares a process in the tree that is also running since the given time, so a walk up can compare ages.</summary>
    public FakeProcessRegistry InTree(int processId, string name, int parentId, DateTimeOffset startedAt)
    {
        return InTree(processId, name, parentId).Running(processId, startedAt);
    }

    /// <summary>Declares what a process uses; one declared in the tree but not here reads as refusing the query.</summary>
    public FakeProcessRegistry Using(int processId, DateTimeOffset startedAt, MemorySize privateMemory, string label = "")
    {
        usage[processId] = new ProcessUsage(startedAt, privateMemory, label);
        return this;
    }

    public List<int> Terminated { get; } = [];

    public List<int> EndedCleanly { get; } = [];

    /// <summary>How many times the table was read.</summary>
    public int Snapshots { get; private set; }

    public DateTimeOffset? StartTimeOf(int processId)
    {
        return running.TryGetValue(processId, out DateTimeOffset startedAt) ? startedAt : null;
    }

    public void Terminate(ConfirmedProcess process)
    {
        Terminated.Add(process.Id);
        foreach (int id in DescendantsOf(process.Id).Append(process.Id))
        {
            running.Remove(id);
            tree.Remove(id);
        }
    }

    public ProcessParent? ParentOf(int processId)
    {
        if (!tree.TryGetValue(processId, out (int ParentId, string Name) entry) || !tree.TryGetValue(entry.ParentId, out (int ParentId, string Name) parent))
        {
            return null;
        }

        return new ProcessParent(entry.ParentId, parent.Name);
    }

    /// <summary>Called as each process is ended cleanly, so a test can put it in order with other fakes' records.</summary>
    public Action<int> OnEndCleanly { get; set; } = _ => { };

    public void EndCleanly(ConfirmedProcess process)
    {
        EndedCleanly.Add(process.Id);
        OnEndCleanly(process.Id);
        running.Remove(process.Id);
    }

    public IReadOnlyList<ProcessFacts> Snapshot()
    {
        Snapshots++;

        IEnumerable<ProcessFacts> declared = tree.Select(entry => new ProcessFacts(entry.Key, entry.Value.ParentId, entry.Value.Name));
        IEnumerable<ProcessFacts> runningOnly = running.Keys.Where(id => !tree.ContainsKey(id)).Select(id => new ProcessFacts(id, 0, string.Empty));

        return [.. declared, .. runningOnly];
    }

    private List<int> DescendantsOf(int processId)
    {
        List<int> found = [];
        Queue<int> pending = new([processId]);
        while (pending.TryDequeue(out int parent))
        {
            IEnumerable<int> children = tree.Where(entry => entry.Value.ParentId == parent && entry.Key != parent).Select(entry => entry.Key);
            foreach (int child in children.Where(child => !found.Contains(child)))
            {
                found.Add(child);
                pending.Enqueue(child);
            }
        }

        return found;
    }

    public ProcessUsage? UsageOf(int processId)
    {
        return usage.TryGetValue(processId, out ProcessUsage? found) ? found : null;
    }
}