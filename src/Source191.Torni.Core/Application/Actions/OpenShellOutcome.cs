using Source191.Torni.Application.Effects;

namespace Source191.Torni.Application.Actions;

/// <summary>A plain shell tab was opened in the worktree.</summary>
public sealed record OpenShellOutcome(string FolderName) : Outcome;