using Source191.Vantage.Application.Effects;
using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;
using Source191.Vantage.Ports;

namespace Source191.Vantage.Application.Actions;

/// <summary>Prunes a repository's stale worktree registrations and says how many went.</summary>
public sealed class PruneRepositoryHandler(IGit git) : IEffectHandler<PruneRepository>
{
    public async Task<string> HandleAsync(PruneRepository effect, CancellationToken cancellationToken)
    {
        int pruned = await git.PruneWorktreesAsync(effect.Repository, cancellationToken);

        return $"Pruned {Registrations.Count(pruned)} from {new RepositoryPath(effect.Repository).Name}.";
    }
}

/// <summary>"1 stale registration" or "3 stale registrations", as both prune paths word it.</summary>
internal static class Registrations
{
    public static string Count(int number)
    {
        return number == 1 ? "1 stale registration" : $"{number} stale registrations";
    }
}