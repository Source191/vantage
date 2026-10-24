namespace Source191.Vantage.Domain;

/// <summary>A process's parent: its identifier and executable name without extension (for example WindowsTerminal, pwsh).</summary>
public sealed record ProcessParent(int ProcessId, string Name);

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
}