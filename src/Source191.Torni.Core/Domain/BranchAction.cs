namespace Source191.Torni.Domain;

/// <summary>What removing a worktree does with its branch.</summary>
public enum BranchAction
{

    /// <summary>Deleted without asking: a merged branch created here.</summary>
    Delete,

    /// <summary>The user is asked whether to delete it.</summary>
    Ask,
}