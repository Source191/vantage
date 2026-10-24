using Source191.Vantage.Application.Effects;
using Source191.Vantage.Application.Views;
using Source191.Vantage.Ports;

namespace Source191.Vantage.Application.Actions;

/// <summary>Deletes a branch the user agreed to lose, with force because it is not merged by definition.</summary>
public sealed class DeleteBranchHandler(IGit git) : IEffectHandler<DeleteBranch>
{
    public async Task<string> HandleAsync(DeleteBranch effect, CancellationToken cancellationToken)
    {
        await git.DeleteBranchAsync(effect.Branch.Repository, effect.Branch.Branch, force: true, cancellationToken);

        return "Branch deleted.";
    }
}