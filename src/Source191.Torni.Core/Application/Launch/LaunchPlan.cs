using Source191.Torni.Domain;

namespace Source191.Torni.Application.Launch;

/// <summary>What the launch service will do: where the session starts, under what name, and whether a worktree is made first (<see cref="LaunchPlace"/>).</summary>
public sealed record LaunchPlan(
    FolderPath Directory,
    SessionName Name,
    LaunchPlace Place,
    WorktreeBase Base,
    OpeningPrompt OpeningPrompt,
    RepositoryChoice Repository);