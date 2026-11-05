using Source191.Torni.Domain;

namespace Source191.Torni.Ports;

/// <summary>What the CLI's listing holds: the sessions, and apart from them the automated runs.</summary>
public sealed record SessionListing(IReadOnlyList<Session> Sessions, IReadOnlyList<AutomatedRun> AutomatedRuns)
{
    public static SessionListing Empty { get; } = new([], []);
}