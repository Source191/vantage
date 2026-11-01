using Source191.Vantage.Application.Actions;
using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;
using Source191.Vantage.Ports;
using System.Diagnostics;

namespace Source191.Vantage.Application.Launch;

/// <summary>
/// <c>vantage here</c> from start to finish: plan from the name and prompt the command line parsed, start the supervised session in the folder,
/// attach in this tab, and exit 0 after a detach, 1 when the start failed, 2 for a refused name.
/// </summary>
public sealed class HereCommandRunner(IFolders folders, LaunchService launches, TerminalEffects effects, ITerminalConsole console, TextWriter output, TextWriter error)
{
    public async Task<int> RunAsync(string directory, string? name, string? prompt, CancellationToken cancellationToken)
    {
        return HereCommand.Plan(directory, name, prompt, IsRepository) switch
        {
            LaunchPlanResult.Planned planned => await LaunchAsync(planned.Plan, directory, cancellationToken),
            LaunchPlanResult.Refused refused => await RefuseAsync(refused.Problem),
            _ => throw new UnreachableException(),
        };
    }

    private async Task<int> LaunchAsync(LaunchPlan plan, string directory, CancellationToken cancellationToken)
    {
        LaunchOutcome outcome = await launches.LaunchAsync(plan, cancellationToken);
        string said = Wording.Here(outcome, plan.Name.Value, directory);

        return outcome switch
        {
            LaunchOutcome.Started started => await AttachAsync(said, started.JobId, cancellationToken),
            LaunchOutcome.WorkspaceNotTrusted or LaunchOutcome.Failed => await NotStartedAsync(said),
            _ => throw new UnreachableException(),
        };
    }

    /// <summary>Listed yet or not, the session is attached to in this tab.</summary>
    private async Task<int> AttachAsync(string said, JobId jobId, CancellationToken cancellationToken)
    {
        await output.WriteLineAsync(said);
        await effects.RunAsync(new AttachToSession(jobId), cancellationToken);
        console.RecoverAfterChild();
        return 0;
    }

    /// <summary>Not trusted or failed: Wording says which.</summary>
    private async Task<int> NotStartedAsync(string said)
    {
        await error.WriteLineAsync(said);
        return 1;
    }

    private async Task<int> RefuseAsync(LaunchProblem problem)
    {
        await error.WriteLineAsync(Wording.Describe(problem));
        return 2;
    }

    private bool IsRepository(string path)
    {
        string git = Path.Combine(path, ".git");

        return folders.DirectoryExists(git) || folders.FileExists(git);
    }
}