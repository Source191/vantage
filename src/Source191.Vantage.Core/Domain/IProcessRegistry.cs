namespace Source191.Vantage.Domain;

/// <summary>A process's parent: its identifier and executable name without extension (for example WindowsTerminal, pwsh).</summary>
public sealed record ProcessParent(int ProcessId, string Name);

/// <summary>A process as the operating system's table lists it: its id, its parent's id and its executable name without the extension.</summary>
public sealed record ProcessFacts(int ProcessId, int ParentId, string Name);

/// <summary>What one running process uses: when it started (UTC), its private working set, and the command line it was started with.</summary>
public sealed record ProcessUsage(DateTimeOffset StartedAt, MemorySize PrivateMemory, string CommandLine);

/// <summary>
/// The operating system's view of running processes. Implemented at the boundary; faked in tests.
/// </summary>
public interface IProcessRegistry
{
    DateTimeOffset? StartTimeOf(int processId);

    /// <summary>Ends the process and everything it started. A process that has already gone is not an error.</summary>
    void Terminate(int processId);

    /// <summary>The parent of a running process, or null when either has gone.</summary>
    ProcessParent? ParentOf(int processId);

    /// <summary>
    /// Ends a process with exit code 0, as if it had finished normally. Windows Terminal closes a tab whose shell
    /// exits cleanly and keeps one whose shell was killed, so this is how a tab is closed from outside.
    /// </summary>
    void EndCleanly(int processId);

    /// <summary>Every process on the machine, with its parent's id, read in one go without opening any of them.</summary>
    IReadOnlyList<ProcessFacts> Snapshot();

    /// <summary>What a process uses now, or null when it has gone or refuses to be read.</summary>
    ProcessUsage? UsageOf(int processId);
}