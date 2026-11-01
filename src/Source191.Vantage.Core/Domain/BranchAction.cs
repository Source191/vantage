namespace Source191.Vantage.Domain;

/// <summary>What removing a worktree does with its branch.</summary>
public enum BranchAction
{
    /// <summary>Nothing: the removal was refused, or only a leftover folder goes.</summary>
    None,

    /// <summary>Deleted without asking: a merged branch Vantage created.</summary>
    Delete,

    /// <summary>The user is asked whether to delete it.</summary>
    Ask,
}