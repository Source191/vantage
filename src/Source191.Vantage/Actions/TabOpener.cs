using Source191.Vantage.Adapters.State;
using Source191.Vantage.Adapters.Terminal;
using Source191.Vantage.Domain;
using Source191.Vantage.Screens;

namespace Source191.Vantage.Actions;

/// <summary>Opens a session in its own Windows Terminal tab and records the tab so its colour and liveness are known.</summary>
public sealed class TabOpener(StateStore store, IProcessRegistry processes, IWindowsTerminal terminal, TimeProvider clock, string claudeExecutable, string vantageExecutable)
{
    private readonly TabColourSelection colours = new(TabPalette.Default);

    public async Task<string> OpenAsync(SessionRow row, CancellationToken cancellationToken)
    {
        if (row.HasLiveTab)
        {
            return $"{row.Session.Name} is already open in another tab.";
        }

        return await OpenAsync(row.Session, cancellationToken);
    }

    /// <summary>Opens a tab for a session that is not a listed row yet, such as one the wizard has just started.</summary>
    public async Task<string> OpenAsync(Session session, CancellationToken cancellationToken)
    {
        if (session.JobId is null)
        {
            return "Only background sessions can be opened in a tab.";
        }

        DateTimeOffset now = clock.GetUtcNow();
        IReadOnlyList<TabRegistration> live = new TabRegistrations(await store.ReadTabsAsync()).Live(now, processes);
        ColourChoice colour = colours.Choose([.. live.Select(registration => registration.Colour)]);
        TabRegistration pending = TabRegistration.Pending(session.SessionId, colour.Colour, now);
        await store.WriteTabAsync(pending);

        // The shell passes its own $PID so the registration is tied to the tab, not to the short-lived claim-tab process.
        // The profile is loaded (so the shell behind the session is the normal one) and its output cleared before attaching.
        string script = string.Join('\n',
            $"{PowerShellScript.Invoke(vantageExecutable, ["claim-tab", pending.Id.ToString("N")])} $PID",
            "Clear-Host",
            PowerShellScript.Invoke(claudeExecutable, ["attach", session.JobId]));
        await terminal.OpenTabAsync(new TabLaunch(session.Name, colour.Colour, session.WorkingDirectory, script), cancellationToken);

        string warning = colour.Reused ? " (every palette colour is in use, so one was reused)" : string.Empty;

        return $"Opened {session.Name} in a {colour.Colour.Name.ToLowerInvariant()} tab{warning}.";
    }
}

/// <summary>What "vantage claim-tab" does inside the new tab: stamps the registration with the tab shell's process.</summary>
public sealed class TabClaimer(StateStore store, IProcessRegistry processes)
{
    public async Task ClaimAsync(Guid registrationId, int processId)
    {
        TabRegistration? pending = (await store.ReadTabsAsync()).FirstOrDefault(registration => registration.Id == registrationId);
        DateTimeOffset? startedAt = processes.StartTimeOf(processId);
        if (pending is null || startedAt is null)
        {
            return;
        }

        await store.WriteTabAsync(pending.ClaimedBy(processId, startedAt.Value));
    }
}