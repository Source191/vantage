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
    public Task<OpenTabOutcome> OpenAsync(BackgroundSession session, CancellationToken cancellationToken) => OpenAsync(session, colours.Choose, cancellationToken);

    /// <summary>Opens a tab as above, in the preferred colour when no live tab uses it, such as a renamed session's old colour.</summary>
    public Task<OpenTabOutcome> OpenAsync(BackgroundSession session, TabColour preferred, CancellationToken cancellationToken) => OpenAsync(session, inUse => colours.Choose(inUse, preferred), cancellationToken);

    private async Task<OpenTabOutcome> OpenAsync(BackgroundSession session, Func<IReadOnlyList<TabColour>, ColourChoice> choose, CancellationToken cancellationToken)
    {
        DateTimeOffset now = clock.GetUtcNow();
        foreach (TabRegistration earlier in (await tabs.ReadTabsAsync()).Where(tab => tab.Owner == new TabOwner.ForSession(session.SessionId)))
        {
            await tabs.RemoveTabAsync(earlier.Id);
        }

        IReadOnlyList<TabRegistration> live = new TabRegistrations(await tabs.ReadTabsAsync()).Live(now, processes);
        TabColour[] inUse = [.. live.Select(registration => registration.Colour)];
        ColourChoice colour = choose(inUse);
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

        bool found = await terminal.FocusTabAsync(registration.OwningTerminal(processes), session.Name, cancellationToken);

        return found
            ? new OpenTabOutcome.SwitchedTo(session.Name)
            : new OpenTabOutcome.NotFound(session.Name);
    }
}