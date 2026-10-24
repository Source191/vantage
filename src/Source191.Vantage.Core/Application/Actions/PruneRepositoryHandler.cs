using Source191.Vantage.Application.Effects;
using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;
using Source191.Vantage.Ports;

namespace Source191.Vantage.Application.Actions;

/// <summary>Prunes a repository's stale worktree registrations and says how many went.</summary>
public sealed class PruneRepositoryHandler(IGit git) : IEffectHandler<PruneRepository>
{
    public async Task<Outcome> HandleAsync(PruneRepository effect, CancellationToken cancellationToken)
    {
        int pruned = await git.PruneWorktreesAsync(effect.Repository, cancellationToken);

        return new PruneRepositoryOutcome(pruned, new FolderPath(effect.Repository).Name);
    }
}


/// <summary>How many stale registrations a prune removed from the repository.</summary>
public sealed record PruneRepositoryOutcome(int Pruned, string RepositoryName) : Outcome;