using Source191.Vantage.Domain;
using System.Diagnostics;

namespace Source191.Vantage.Application.Launch;

/// <summary>
/// <c>vantage here [name] [-p prompt]</c>: a supervised session in the current folder, on its current
/// branch, attached in the same tab. Sessions started this way are Vantage-managed from the outset.
/// </summary>
public static class HereCommand
{
    /// <summary>The plan for a name (the folder's unless one was typed) and an opening prompt (none when blank); the command line is parsed before this.</summary>
    public static LaunchPlanResult Plan(string directory, HereName name, string prompt, Func<string, bool> isRepository)
    {
        FolderPath path = FolderPath.From(directory);
        RepositoryChoice repository = new(path.Name, path, isRepository(directory));
        LaunchRequest request = new(repository, NameText(name, path), LaunchMode.CurrentBranch, WorktreeBase.RemoteDefault, prompt);

        // On the current branch no worktree is placed, so the folder itself stands in for the worktree root.
        return LaunchPlanBuilder.Build(request, worktreeRoot: path, _ => false);
    }

    private static string NameText(HereName name, FolderPath folder) => name switch
    {
        HereName.FolderName => folder.Name,
        HereName.Typed typed => typed.Text,
        _ => throw new UnreachableException(),
    };
}