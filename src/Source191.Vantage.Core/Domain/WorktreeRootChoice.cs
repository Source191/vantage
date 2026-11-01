namespace Source191.Vantage.Domain;

/// <summary>Where new worktrees go: a folder chosen, or the default beside the first repository root (inside it when it has no parent, such as <c>D:\</c>).</summary>
public abstract record WorktreeRootChoice
{
    public sealed record BesideFirstRoot : WorktreeRootChoice;

    public sealed record At(FolderPath Folder) : WorktreeRootChoice;
}