using Source191.Vantage.Application.Effects;

namespace Source191.Vantage.Application.Actions;

/// <summary>How many stale registrations a prune removed from the repository.</summary>
public sealed record PruneRepositoryOutcome(int Pruned, string RepositoryName) : Outcome;