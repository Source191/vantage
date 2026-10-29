using Source191.Vantage.Domain;

namespace Source191.Vantage.TestDoubles;

/// <summary>
/// A stand-in for the operating system's process table: the only boundary the tab registry and the session actions touch.
/// </summary>
public sealed class FakeProcessRegistry : IProcessRegistry
{
    private readonly Dictionary<int, DateTimeOffset> running = [];
    private readonly Dictionary<int, (int ParentId, string Name)> tree = [];

    public FakeProcessRegistry Running(int processId, DateTimeOffset startedAt)
    {
        running[processId] = startedAt;
        return this;
    }

    /// <summary>Declares a process, its name and its parent, so a tab's shell can be found by walking up.</summary>
    public FakeProcessRegistry InTree(int processId, string name, int parentId)
    {
        tree[processId] = (parentId, name);
        return this;
    }

    public List<int> Terminated { get; } = [];

    public List<int> EndedCleanly { get; } = [];

    public DateTimeOffset? StartTimeOf(int processId)
    {
        return running.TryGetValue(processId, out DateTimeOffset startedAt) ? startedAt : null;
    }

    public void Terminate(int processId)
    {
        Terminated.Add(processId);
        running.Remove(processId);
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

    public void EndCleanly(int processId)
    {
        EndedCleanly.Add(processId);
        OnEndCleanly(processId);
        running.Remove(processId);
    }
}