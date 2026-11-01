using Source191.Vantage.Application.Effects;

namespace Source191.Vantage.Application.Actions;

/// <summary>A plain shell tab was opened in the worktree.</summary>
public sealed record OpenShellOutcome(string FolderName) : Outcome;