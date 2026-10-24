using Source191.Vantage.Application.Effects;
using Source191.Vantage.Application.Views;
using Source191.Vantage.Ports;

namespace Source191.Vantage.Application.Actions;

/// <summary>Deletes a branch the user agreed to lose, with force because it is not merged by definition.</summary>
public sealed class DeleteBranchHandler(IGit git) : IEffectHandler<DeleteBranch>
{
    public async Task<Outcome> HandleAsync(DeleteBranch effect, CancellationToken cancellationToken)
    {
        await git.DeleteBranchAsync(effect.Branch.Repository.Value, effect.Branch.Branch.Value, force: true, cancellationToken);

        return new DeleteBranchOutcome();
    }
}

/// <summary>The branch the user agreed to lose was deleted.</summary>
public sealed record DeleteBranchOutcome : Outcome;