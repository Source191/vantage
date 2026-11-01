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
    /// The Windows Terminal window that holds this tab, found by walking up from the tab's shell. Any window for a
    /// pending registration or when the chain does not reach Windows Terminal within a few steps.
    /// </summary>
    public TerminalWindow OwningTerminal(IProcessRegistry processes) => Claim switch
    {
        TabClaim.Claimed claimed => WindowOf(TerminalTab.TerminalOf(claimed.ProcessId, processes)),
        TabClaim.Pending => new TerminalWindow.Any(),
        _ => throw new UnreachableException(),
    };

    private static TerminalWindow WindowOf(int? terminal) => terminal is int processId ? new TerminalWindow.Owning(processId) : new TerminalWindow.Any();

    public bool IsLive(DateTimeOffset now, IProcessRegistry processes) => Claim switch
    {
        TabClaim.Pending => now - CreatedAt < PendingGracePeriod,
        TabClaim.Claimed claimed => processes.StartTimeOf(claimed.ProcessId) == claimed.StartedAt,
        _ => throw new UnreachableException(),
    };
}