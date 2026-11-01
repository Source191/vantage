namespace Source191.Vantage.Domain;

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
    /// Ends a process with exit code 0, as if it had finished normally.
    /// <see href="https://learn.microsoft.com/windows/terminal/customize-settings/profile-advanced">Windows Terminal closes
    /// a tab whose shell exits cleanly</see> and keeps one whose shell was killed, so this is how a tab is closed from
    /// outside.
    /// </summary>
    void EndCleanly(int processId);

    /// <summary>Every process on the machine, with its parent's id, read in one go without opening any of them.</summary>
    IReadOnlyList<ProcessFacts> Snapshot();

    /// <summary>What a process uses now, or null when it has gone or refuses to be read.</summary>
    ProcessUsage? UsageOf(int processId);
}