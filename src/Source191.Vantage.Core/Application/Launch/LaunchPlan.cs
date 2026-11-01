using Source191.Vantage.Domain;

namespace Source191.Vantage.Application.Launch;

/// <summary>What the launch service will do: where the session starts, under what name, and whether a worktree is made first (<see cref="LaunchPlace"/>).</summary>
public sealed record LaunchPlan(
    FolderPath Directory,
    SessionName Name,
    LaunchPlace Place,
    WorktreeBase Base,
    OpeningPrompt OpeningPrompt,
    RepositoryChoice Repository);