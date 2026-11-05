namespace Source191.Torni.Domain;

/// <summary>A branch a removal left behind that only the user may delete: the repository it lives in and its name.</summary>
public sealed record BranchToConfirm(FolderPath Repository, BranchName Branch);