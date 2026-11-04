using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Source191.Torni.Domain;

/// <summary>
/// A Windows Terminal tab that Torni opened. Pending until the tab's shell claims it with its own process.
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
        TabClaim.Claimed => TryConfirmShell(processes, out ConfirmedProcess? shell) ? WindowOf(TerminalTab.TerminalOf(shell, processes)) : new TerminalWindow.Any(),
        TabClaim.Pending => new TerminalWindow.Any(),
        _ => throw new UnreachableException(),
    };

    private static TerminalWindow WindowOf(int? terminal) => terminal is int processId ? new TerminalWindow.Owning(processId) : new TerminalWindow.Any();

    public bool IsLive(DateTimeOffset now, IProcessRegistry processes) => Claim switch
    {
        TabClaim.Pending => now - CreatedAt < PendingGracePeriod,
        TabClaim.Claimed => TryConfirmShell(processes, out _),
        _ => throw new UnreachableException(),
    };

    /// <summary>
    /// The tab's shell, when the tab is claimed and the process behind the claimed id started exactly when the claim
    /// recorded; a reused id is another process and is never confirmed.
    /// </summary>
    public bool TryConfirmShell(IProcessRegistry processes, [MaybeNullWhen(false)] out ConfirmedProcess shell)
    {
        if (Claim is not TabClaim.Claimed claimed)
        {
            shell = null;

            return false;
        }

        return ConfirmedProcess.TryConfirmClaim(claimed.ProcessId, processes.StartTimeOf(claimed.ProcessId), claimed.StartedAt, out shell);
    }
}