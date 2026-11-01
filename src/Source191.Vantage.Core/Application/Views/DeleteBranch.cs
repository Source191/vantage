using Source191.Vantage.Domain;

namespace Source191.Vantage.Application.Views;

/// <summary>Delete the branch a removal left behind, after the user agreed.</summary>
public sealed record DeleteBranch(BranchToConfirm Branch) : ViewEffect;