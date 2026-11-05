using Source191.Torni.Domain;
using Source191.Torni.Ports;

namespace Source191.Torni.Application.Actions;

/// <summary>
/// The one place that closes a session's tabs: ends the shell of every live tab the console opened for the session (Windows
/// Terminal closes a tab whose root shell exits with 0) and forgets every registration for it, a pending one too.
/// </summary>
public sealed class SessionTabs(ITabRegistry tabs, IProcessRegistry processes)
{
    public async Task<TabClosure> CloseAsync(SessionId session)
    {
        TabClosure closure = TabClosure.NoTab;
        foreach (TabRegistration tab in new TabRegistrations(await tabs.ReadTabsAsync()).For(session))
        {
            if (tab.TryConfirmShell(processes, out ConfirmedProcess? shell))
            {
                processes.EndCleanly(shell);
                closure = TabClosure.Closed;
            }

            await tabs.RemoveTabAsync(tab.Id);
        }

        return closure;
    }
}