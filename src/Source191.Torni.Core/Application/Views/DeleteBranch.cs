using Source191.Torni.Domain;

namespace Source191.Torni.Application.Views;

/// <summary>Delete the branch a removal left behind, after the user agreed.</summary>
public sealed record DeleteBranch(BranchToConfirm Branch) : ViewEffect;