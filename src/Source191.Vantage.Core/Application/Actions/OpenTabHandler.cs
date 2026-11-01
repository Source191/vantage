using Source191.Vantage.Application.Effects;
using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;
using Source191.Vantage.Ports;
using System.Diagnostics;

namespace Source191.Vantage.Application.Actions;

/// <summary>
/// Opens a session in its own Windows Terminal tab and records the tab so its colour and liveness are known; when a
/// live tab already shows the session, switches to it instead.
/// </summary>
public sealed class OpenTabHandler(ITabRegistry tabs, IProcessRegistry processes, IWindowsTerminal terminal, TimeProvider clock, TabScripts scripts) : IEffectHandler<OpenTabForSession>
{
    private readonly TabColourSelection colours = new(TabPalette.Default);

    public async Task<Outcome> HandleAsync(OpenTabForSession effect, CancellationToken cancellationToken)
    {
        return effect.Tab switch
        {
            LiveTab.Open => await SwitchToTabAsync(effect.Session, cancellationToken),
            LiveTab.None => await OpenAsync(effect.Session, cancellationToken),
            _ => throw new UnreachableException(),
        };
    }

    /// <summary>Opens a tab for a session, a listed row or one the wizard has just started, after forgetting any earlier tab registered for it, so that tab's colour is free again.</summary>
    public Task<OpenTabOutcome> OpenAsync(BackgroundSession session, CancellationToken cancellationToken) => OpenAsync(session, preferred: null, cancellationToken);

    /// <summary>Opens a tab as above, in the preferred colour when no live tab uses it, such as a renamed session's old colour.</summary>
    public async Task<OpenTabOutcome> OpenAsync(BackgroundSession session, TabColour? preferred, CancellationToken cancellationToken)
    {
        DateTimeOffset now = clock.GetUtcNow();
        foreach (TabRegistration earlier in (await tabs.ReadTabsAsync()).Where(tab => tab.Owner == new TabOwner.ForSession(session.SessionId)))
        {
            await tabs.RemoveTabAsync(earlier.Id);
        }

        IReadOnlyList<TabRegistration> live = new TabRegistrations(await tabs.ReadTabsAsync()).Live(now, processes);
        TabColour[] inUse = [.. live.Select(registration => registration.Colour)];
        ColourChoice colour = preferred is null ? colours.Choose(inUse) : colours.Choose(inUse, preferred);
        TabRegistration pending = TabRegistration.Pending(new TabOwner.ForSession(session.SessionId), colour.Colour, now);
        await tabs.WriteTabAsync(pending);

        await terminal.OpenTabAsync(scripts.Attach(session, pending), cancellationToken);

        return new OpenTabOutcome.Opened(session.Name, colour.Colour.Name, colour.Reused);
    }

    /// <summary>
    /// The row says a tab is open: when its registration is live now, select it in the Windows Terminal window that owns
    /// its shell (every window while it is still being claimed); otherwise forget it and open a new tab.
    /// </summary>
    private async Task<OpenTabOutcome> SwitchToTabAsync(BackgroundSession session, CancellationToken cancellationToken)
    {
        TabRegistration? registration = (await tabs.ReadTabsAsync()).FirstOrDefault(tab => tab.Owner == new TabOwner.ForSession(session.SessionId));
        if (registration is null || !registration.IsLive(clock.GetUtcNow(), processes))
        {
            return await OpenAsync(session, cancellationToken);
        }

        int? owner = registration.OwningTerminal(processes);
        bool found = await terminal.FocusTabAsync(owner, session.Name, cancellationToken);

        return found
            ? new OpenTabOutcome.SwitchedTo(session.Name)
            : new OpenTabOutcome.NotFound(session.Name);
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
    /// <summary>Opened in the named colour; reused when every palette colour was already taken.</summary>
    public sealed record Opened(string Name, string Colour, bool ColourReused) : OpenTabOutcome;

    public sealed record SwitchedTo(string Name) : OpenTabOutcome;

    public sealed record NotFound(string Name) : OpenTabOutcome;
}