using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;
using System.Collections.Concurrent;

namespace Source191.Vantage.Application;

/// <summary>
/// When each background session was last prompted, as far as this Vantage saw: the last time it moved from needs input
/// or idle to working between two refreshes. Kept in memory for the run; nothing is written anywhere. Observed on the UI
/// thread and read by the detail loader off it.
/// </summary>
public sealed class SessionActivity(TimeProvider clock)
{
    private readonly ConcurrentDictionary<SessionId, DateTimeOffset> prompted = new();

    public void Observe(IReadOnlyList<SessionRow> previous, IReadOnlyList<SessionRow> current)
    {
        ArgumentNullException.ThrowIfNull(previous);
        ArgumentNullException.ThrowIfNull(current);
        Dictionary<SessionId, DisplayStateKind> before = previous.ToDictionary(row => row.Session.SessionId, row => row.Session.State.Kind);
        DateTimeOffset now = clock.GetUtcNow();
        foreach (SessionRow row in current.Where(row => row.Session is BackgroundSession { State.Kind: DisplayStateKind.Working } && WasWaiting(before, row.Session.SessionId)))
        {
            prompted[row.Session.SessionId] = now;
        }
    }

    public DateTimeOffset? LastPromptedAt(SessionId session) => prompted.TryGetValue(session, out DateTimeOffset at) ? at : null;

    private static bool WasWaiting(Dictionary<SessionId, DisplayStateKind> before, SessionId session) =>
        before.TryGetValue(session, out DisplayStateKind was) && was is DisplayStateKind.NeedsInput or DisplayStateKind.Idle;
}