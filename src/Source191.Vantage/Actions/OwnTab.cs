using Source191.Vantage.Adapters.State;
using Source191.Vantage.Domain;

namespace Source191.Vantage.Actions;

/// <summary>
/// Vantage's own tab: registered like a session tab, keyed "vantage" and claimed by Vantage's own process, so the
/// colour it takes (green when free) is skipped by session tabs and the registration goes stale with the process.
/// </summary>
public sealed class OwnTab(StateStore store, IProcessRegistry processes, TimeProvider clock)
{
    public const string RegistrationKey = "vantage";

    private static readonly TabColour Preferred = TabPalette.Default.Single(colour => colour.Name == "Green");

    private readonly TabColourSelection colours = new(TabPalette.Default);

    /// <summary>The colour the tab should take, or null when the process cannot be registered.</summary>
    public async Task<TabColour?> ClaimAsync(int processId)
    {
        DateTimeOffset? startedAt = processes.StartTimeOf(processId);
        if (startedAt is null)
        {
            return null;
        }

        DateTimeOffset now = clock.GetUtcNow();
        IReadOnlyList<TabRegistration> live = new TabRegistrations(await store.ReadTabsAsync()).Live(now, processes);
        ColourChoice choice = colours.Choose([.. live.Select(registration => registration.Colour)], Preferred);
        await store.WriteTabAsync(TabRegistration.Pending(RegistrationKey, choice.Colour, now).ClaimedBy(processId, startedAt.Value));

        return choice.Colour;
    }
}