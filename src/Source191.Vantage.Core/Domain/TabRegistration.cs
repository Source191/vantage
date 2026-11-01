using System.Diagnostics;

namespace Source191.Vantage.Domain;

/// <summary>
/// A Windows Terminal tab that Vantage opened. Pending until the tab's shell claims it with its own process.
/// </summary>
public sealed record TabRegistration(
    Guid Id,
    TabOwner Owner,
    TabColour Colour,
    DateTimeOffset CreatedAt,
    TabClaim Claim)
{
    private static readonly TimeSpan PendingGracePeriod = TimeSpan.FromSeconds(60);

    public static TabRegistration Pending(TabOwner owner, TabColour colour, DateTimeOffset createdAt) => new(Guid.NewGuid(), owner, colour, createdAt, new TabClaim.Pending());

    public TabRegistration ClaimedBy(int processId, DateTimeOffset processStartedAt) => this with { Claim = new TabClaim.Claimed(processId, processStartedAt) };

    /// <summary>
    /// The Windows Terminal process whose window holds this tab, found by walking up from the tab's shell. Null for a
    /// pending registration or when the chain does not reach Windows Terminal within a few steps.
    /// </summary>
    public int? OwningTerminal(IProcessRegistry processes) => Claim is TabClaim.Claimed claimed ? TerminalTab.TerminalOf(claimed.ProcessId, processes) : null;

    public bool IsLive(DateTimeOffset now, IProcessRegistry processes) => Claim switch
    {
        TabClaim.Pending => now - CreatedAt < PendingGracePeriod,
        TabClaim.Claimed claimed => processes.StartTimeOf(claimed.ProcessId) == claimed.StartedAt,
        _ => throw new UnreachableException(),
    };
}

/// <summary>Whose tab a registration is: a session's, or Vantage's own.</summary>
public abstract record TabOwner
{
    public static readonly TabOwner Vantage = new ForVantage();

    public sealed record ForSession(SessionId Id) : TabOwner;

    /// <summary>The tab Vantage itself runs in, so its colour is not handed to a session tab.</summary>
    public sealed record ForVantage : TabOwner;
}

public sealed class TabRegistrations(IReadOnlyList<TabRegistration> items)
{
    public IReadOnlyList<TabRegistration> Live(DateTimeOffset now, IProcessRegistry processes) => [.. items.Where(registration => registration.IsLive(now, processes))];
}