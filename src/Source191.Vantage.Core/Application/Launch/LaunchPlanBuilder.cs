using Source191.Vantage.Domain;
using System.Diagnostics;

namespace Source191.Vantage.Application.Launch;

public static class LaunchPlanBuilder
{
    public static LaunchPlanResult Build(LaunchRequest request, FolderPath worktreeRoot, Func<string, bool> folderExists)
    {
        return SessionName.Validate(request.NameText) switch
        {
            SessionNameValidation.Valid valid => Build(request, valid.Name, worktreeRoot, folderExists),
            SessionNameValidation.Refused refused => new LaunchPlanResult.Refused(new LaunchProblem.InvalidName(refused.Problem)),
            _ => throw new UnreachableException(),
        };
    }

    private static LaunchPlanResult Build(LaunchRequest request, SessionName name, FolderPath worktreeRoot, Func<string, bool> folderExists)
    {
        OpeningPrompt prompt = Blank(request.OpeningPromptText) ? new OpeningPrompt.None() : new OpeningPrompt.Text(request.OpeningPromptText.Trim());
        if (prompt is OpeningPrompt.Text { Words: string text } && text.StartsWith('-'))
        {
            return new LaunchPlanResult.Refused(new LaunchProblem.PromptLooksLikeAnOption());
        }

        if (request.Mode == LaunchMode.CurrentBranch)
        {
            return new LaunchPlanResult.Planned(new LaunchPlan(request.Repository.Path, name, new LaunchPlace.CurrentBranch(), request.Base, prompt, request.Repository));
        }

        if (!request.Repository.IsRepository)
        {
            return new LaunchPlanResult.Refused(new LaunchProblem.WorktreeNeedsRepository());
        }

        if (!WorktreeSlug.TryParse(name.Value, out WorktreeSlug? slug))
        {
            return new LaunchPlanResult.Refused(new LaunchProblem.WorktreeNeedsLetters());
        }

        PlannedWorktree worktree = PlannedWorktree.For(worktreeRoot, request.Repository.Path, slug);
        LaunchPlace place = folderExists(worktree.Path.Value) ? new LaunchPlace.ExistingWorktree(worktree) : new LaunchPlace.NewWorktree(worktree);

        return new LaunchPlanResult.Planned(new LaunchPlan(worktree.Path, name, place, request.Base, prompt, request.Repository));
    }

    private static bool Blank(string text) => string.IsNullOrWhiteSpace(text);
}