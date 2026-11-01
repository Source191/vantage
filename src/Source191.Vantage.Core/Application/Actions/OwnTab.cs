using Source191.Vantage.Domain;
using Source191.Vantage.Ports;

namespace Source191.Vantage.Application.Actions;

/// <summary>
/// Vantage's own tab: registered like a session tab, owned by <see cref="TabOwner.Vantage"/> and claimed by Vantage's own process, so the
/// colour it takes (green when free) is skipped by session tabs and the registration goes stale with the process.
/// </summary>
public sealed class OwnTab(ITabRegistry tabs, IProcessRegistry processes, TimeProvider clock)
{
    private static readonly TabColour Preferred = TabPalette.Default.Single(colour => colour.Name == "Green");

    private readonly TabColourSelection colours = new(TabPalette.Default);

    /// <summary>The colour the tab should take, or unregistered when the process's start time cannot be read.</summary>
    public async Task<OwnTabClaim> ClaimAsync(int processId)
    {
        DateTimeOffset? startedAt = processes.StartTimeOf(processId);
        if (startedAt is null)
        {
            return new OwnTabClaim.Unregistered();
        }

        DateTimeOffset now = clock.GetUtcNow();
        IReadOnlyList<TabRegistration> live = new TabRegistrations(await tabs.ReadTabsAsync()).Live(now, processes);
        ColourChoice choice = colours.Choose([.. live.Select(registration => registration.Colour)], Preferred);
        await tabs.WriteTabAsync(TabRegistration.Pending(TabOwner.Vantage, choice.Colour, now).ClaimedBy(processId, startedAt.Value));

        return new OwnTabClaim.Claimed(choice.Colour);
    }
}

/// <summary>Vantage's own tab claimed in a colour, or left unregistered, when it is titled alone.</summary>
public abstract record OwnTabClaim
{
    public sealed record Claimed(TabColour Colour) : OwnTabClaim;

    public sealed record Unregistered : OwnTabClaim;
}