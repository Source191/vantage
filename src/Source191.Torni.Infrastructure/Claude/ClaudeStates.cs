using Source191.Torni.Domain;

namespace Source191.Torni.Adapters.Claude;

/// <summary>
/// The Claude CLI's vocabulary, read where it arrives. The listing's
/// <see href="https://code.claude.com/docs/en/agent-view">documented states</see> are working, blocked, done, failed
/// and stopped; status (busy, waiting, idle) says what the process is doing right now and wins while it is alive.
/// </summary>
public static class ClaudeStates
{
    public static DisplayState From(int? processId, string? status, string? state) => (processId, status, state) switch
    {
        (null, _, "failed") => DisplayState.Failed,
        (null, _, _) => DisplayState.Stopped,
        (_, null, _) => new DisplayState.Starting(),
        (_, "busy", "working" or null) => DisplayState.Working,
        (_, "waiting", _) => DisplayState.NeedsInput,
        (_, "idle", "blocked") => DisplayState.NeedsInput,
        (_, "idle", "done") => DisplayState.Completed,
        (_, "idle", "working" or null) => DisplayState.Idle,
        _ => new DisplayState.Unknown($"{status}/{state}"),
    };
}