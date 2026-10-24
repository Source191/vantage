using Source191.Vantage.Application.Effects;
using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;
using Source191.Vantage.Ports;

namespace Source191.Vantage.Application.Actions;

/// <summary>
/// Opens a session in its own Windows Terminal tab and records the tab so its colour and liveness are known; when a
/// live tab already shows the session, switches to it instead.
/// </summary>
public sealed class OpenTabHandler(ITabRegistry tabs, IProcessRegistry processes, IWindowsTerminal terminal, TimeProvider clock, string claudeExecutable, string vantageExecutable) : IEffectHandler<OpenTabForSession>
{
    private readonly TabColourSelection colours = new(TabPalette.Default);

    public Task<string> HandleAsync(OpenTabForSession effect, CancellationToken cancellationToken)
    {
        return effect.Row.HasLiveTab
            ? SwitchToTabAsync(effect.Row.Session, cancellationToken)
            : OpenAsync(effect.Row.Session, cancellationToken);
    }

    /// <summary>Opens a tab for a session that is not a listed row yet, such as one the wizard has just started.</summary>
    public async Task<string> OpenAsync(Session session, CancellationToken cancellationToken)
    {
        if (session.JobId is null)
        {
            return "Only background sessions can be opened in a tab.";
        }

        DateTimeOffset now = clock.GetUtcNow();
        IReadOnlyList<TabRegistration> live = new TabRegistrations(await tabs.ReadTabsAsync()).Live(now, processes);
        ColourChoice colour = colours.Choose([.. live.Select(registration => registration.Colour)]);
        TabRegistration pending = TabRegistration.Pending(session.SessionId, colour.Colour, now);
        await tabs.WriteTabAsync(pending);

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

    /// <summary>The tab is already open: select it in the Windows Terminal window that owns its shell.</summary>
    private async Task<string> SwitchToTabAsync(Session session, CancellationToken cancellationToken)
    {
        TabRegistration? registration = (await tabs.ReadTabsAsync()).FirstOrDefault(tab => tab.SessionId == session.SessionId);
        int? owner = registration?.OwningTerminal(processes);
        bool found = await terminal.FocusTabAsync(owner, session.Name, cancellationToken);

        return found
            ? $"Switched to the tab of {session.Name}."
            : $"{session.Name} is open in another tab, but Vantage could not find it. Is it in another window?";
    }
}

/// <summary>What "vantage claim-tab" does inside the new tab: stamps the registration with the tab shell's process.</summary>
public sealed class TabClaimer(ITabRegistry tabs, IProcessRegistry processes)
{
    public async Task ClaimAsync(Guid registrationId, int processId)
    {
        TabRegistration? pending = (await tabs.ReadTabsAsync()).FirstOrDefault(registration => registration.Id == registrationId);
        DateTimeOffset? startedAt = processes.StartTimeOf(processId);
        if (pending is null || startedAt is null)
        {
            return;
        }

        await tabs.WriteTabAsync(pending.ClaimedBy(processId, startedAt.Value));
    }
}

/// <summary>What opening a session's tab did: opened one, switched to the open one, or could not.</summary>
public abstract record OpenTabOutcome : Outcome
{
    public sealed record NotBackground : OpenTabOutcome;

    /// <summary>Opened in the named colour; reused when every palette colour was already taken.</summary>
    public sealed record Opened(string Name, string Colour, bool ColourReused) : OpenTabOutcome;

    public sealed record SwitchedTo(string Name) : OpenTabOutcome;

    public sealed record NotFound(string Name) : OpenTabOutcome;
}