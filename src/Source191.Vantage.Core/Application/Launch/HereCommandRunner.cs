using Source191.Vantage.Application.Actions;
using Source191.Vantage.Application.Views;
using Source191.Vantage.Ports;

namespace Source191.Vantage.Application.Launch;

/// <summary>
/// <c>vantage here</c> from start to finish: plan from the arguments, start the supervised session in the folder,
/// attach in this tab, and exit 0 after a detach, 1 when the start failed, 2 for a usage error.
/// </summary>
public sealed class HereCommandRunner(IFolders folders, LaunchService launches, TerminalEffects effects, ITerminalConsole console, TextWriter output, TextWriter error)
{
    public async Task<int> RunAsync(string directory, IReadOnlyList<string> arguments, CancellationToken cancellationToken)
    {
        LaunchPlanResult planned = HereCommand.Plan(directory, arguments, IsRepository);
        if (planned.Plan is not LaunchPlan plan)
        {
            await error.WriteLineAsync(planned.Problem is null ? null : Wording.Describe(planned.Problem));
            return 2;
        }

        LaunchOutcome outcome = await launches.LaunchAsync(plan, cancellationToken);
        switch (outcome.Kind)
        {
            case LaunchOutcomeKind.Started when outcome.JobId is not null:
                await output.WriteLineAsync(Wording.Here(outcome, plan.Name.Text, directory));
                await effects.RunAsync(new AttachToSession(outcome.JobId), cancellationToken);
                console.RecoverAfterChild();
                return 0;
            default:
                // Not trusted or failed: Wording says which.
                await error.WriteLineAsync(Wording.Here(outcome, plan.Name.Text, directory));
                return 1;
        }
    }

    private bool IsRepository(string path)
    {
        string git = Path.Combine(path, ".git");

        return folders.DirectoryExists(git) || folders.FileExists(git);
    }
}