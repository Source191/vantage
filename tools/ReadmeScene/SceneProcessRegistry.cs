using Source191.Torni.Domain;

namespace Source191.Torni.ReadmeScene;

/// <summary>
/// The process table as the scene sees it: the scene's made-up Claude Code processes answered from the listing, every
/// other process (the tab shells, Windows Terminal) from the real table. A scene process is never ended, so an id that
/// happens to belong to a real process is never touched.
/// </summary>
public sealed class SceneProcessRegistry(SceneListing listing, IProcessRegistry real) : IProcessRegistry
{
    private const string ClaudeName = "claude";

    public DateTimeOffset? StartTimeOf(int processId) => listing.UsageOf(processId) is ProcessUsage usage ? usage.StartedAt : real.StartTimeOf(processId);

    public void Terminate(ConfirmedProcess process)
    {
        if (!IsScene(process.Id))
        {
            real.Terminate(process);
        }
    }

    public ProcessParent? ParentOf(int processId) => IsScene(processId) ? null : real.ParentOf(processId);

    public void EndCleanly(ConfirmedProcess process)
    {
        if (!IsScene(process.Id))
        {
            real.EndCleanly(process);
        }
    }

    public IReadOnlyList<ProcessFacts> Snapshot() => [.. real.Snapshot().Where(facts => !IsScene(facts.ProcessId)), .. listing.ProcessIds().Select(processId => new ProcessFacts(processId, 0, ClaudeName))];

    public ProcessUsage? UsageOf(int processId) => listing.UsageOf(processId) ?? real.UsageOf(processId);

    private bool IsScene(int processId) => listing.UsageOf(processId) is not null;
}