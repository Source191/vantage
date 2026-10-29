using Source191.Vantage.Domain;
using Source191.Vantage.Ports;

namespace Source191.Vantage.Application.Actions;

/// <summary>
/// The one place that closes a session's tabs: ends the shell of every live tab Vantage opened for the session (Windows
/// Terminal closes a tab whose root shell exits with 0) and forgets every registration for it, a pending one too.
/// </summary>
public sealed class SessionTabs(ITabRegistry tabs, IProcessRegistry processes, TimeProvider clock)
{
    public async Task<TabClosure> CloseAsync(SessionId session)
    {
        DateTimeOffset now = clock.GetUtcNow();
        TabClosure closure = TabClosure.NoTab;
        foreach (TabRegistration tab in (await tabs.ReadTabsAsync()).Where(tab => tab.Owner == new TabOwner.ForSession(session)))
        {
            if (tab is { ProcessId: int shell } && tab.IsLive(now, processes))
            {
                processes.EndCleanly(shell);
                closure = TabClosure.Closed;
            }

            await tabs.RemoveTabAsync(tab.Id);
        }

        return closure;
    }
}

/// <summary>Whether closing a session's tabs ended a live one.</summary>
public enum TabClosure
{
    Closed,
    NoTab,
}