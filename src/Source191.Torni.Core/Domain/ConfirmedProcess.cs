using System.Diagnostics.CodeAnalysis;

namespace Source191.Torni.Domain;

/// <summary>
/// A process id checked against the start time recorded for it, which is the only kind of process
/// <see cref="IProcessRegistry"/> will end. Windows reuses process ids, so an id alone may now belong to something
/// unrelated; only the three doors here make one of these, each by a rule of its own: the listing's (the process started
/// no later than the session plus a few seconds), the tab claim's (the recorded start time exactly) and the walk's
/// (a parent started no later than its confirmed child).
/// </summary>
public sealed record ConfirmedProcess
{
    private static readonly TimeSpan Rounding = TimeSpan.FromSeconds(5);

    private ConfirmedProcess(int id, DateTimeOffset startedAt)
    {
        Id = id;
        StartedAt = startedAt;
    }

    public int Id { get; }

    public DateTimeOffset StartedAt { get; }

    /// <summary>
    /// A session records its start after its process has started, so the process started no later than the session; one
    /// that started after it is another process given the same id once the session's had gone. A few seconds cover the
    /// rounding of the two clocks.
    /// </summary>
    public static bool TryConfirm(int processId, DateTimeOffset? processStartedAt, DateTimeOffset sessionStartedAt, [MaybeNullWhen(false)] out ConfirmedProcess process)
    {
        process = processStartedAt is DateTimeOffset started && started <= sessionStartedAt + Rounding ? new ConfirmedProcess(processId, started) : null;

        return process is not null;
    }

    /// <summary>A tab's shell is the one process that started at exactly the time the claim recorded.</summary>
    public static bool TryConfirmClaim(int processId, DateTimeOffset? processStartedAt, DateTimeOffset claimedStartedAt, [MaybeNullWhen(false)] out ConfirmedProcess process)
    {
        process = processStartedAt is DateTimeOffset started && started == claimedStartedAt ? new ConfirmedProcess(processId, started) : null;

        return process is not null;
    }

    /// <summary>
    /// Windows never clears a parent id, so a parent that started after its child, or whose start cannot be read, is not
    /// the child's real parent.
    /// </summary>
    public static bool TryConfirmParent(int parentId, DateTimeOffset? parentStartedAt, ConfirmedProcess child, [MaybeNullWhen(false)] out ConfirmedProcess parent)
    {
        parent = parentStartedAt is DateTimeOffset started && started <= child.StartedAt ? new ConfirmedProcess(parentId, started) : null;

        return parent is not null;
    }
}