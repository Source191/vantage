namespace Source191.Torni.Domain;

/// <summary>
/// The operating system's view of running processes. Implemented at the boundary; faked in tests.
/// </summary>
public interface IProcessRegistry
{
    /// <summary>
    /// When the process started, or null when it has gone or refuses to be read; <see cref="ConfirmedProcess"/> and
    /// <see cref="TabRegistration.IsLive"/> rely on the null to tell a process that has gone from the one they recorded.
    /// </summary>
    DateTimeOffset? StartTimeOf(int processId);

    /// <summary>
    /// Ends the process and everything it started. A process that has already gone is not an error. Only a
    /// <see cref="ConfirmedProcess"/> can be ended: an id alone may belong to another process by now.
    /// </summary>
    void Terminate(ConfirmedProcess process);

    /// <summary>The parent of a running process, or null when either has gone.</summary>
    ProcessParent? ParentOf(int processId);

    /// <summary>
    /// Ends a process with exit code 0, as if it had finished normally.
    /// <see href="https://learn.microsoft.com/windows/terminal/customize-settings/profile-advanced">Windows Terminal closes
    /// a tab whose shell exits cleanly</see> and keeps one whose shell was killed, so this is how a tab is closed from
    /// outside. Like <see cref="Terminate"/> it takes only a <see cref="ConfirmedProcess"/>.
    /// </summary>
    void EndCleanly(ConfirmedProcess process);

    /// <summary>Every process on the machine, with its parent's id, read in one go without opening any of them.</summary>
    IReadOnlyList<ProcessFacts> Snapshot();

    /// <summary>What a process uses now, or null when it has gone or refuses to be read.</summary>
    ProcessUsage? UsageOf(int processId);
}