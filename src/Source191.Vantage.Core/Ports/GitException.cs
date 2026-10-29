namespace Source191.Vantage.Ports;

/// <summary>Git failed or answered with something unreadable. InUse: a worktree folder could not be deleted because a tab or program still has it open.</summary>
public sealed class GitException(string message, bool inUse = false) : Exception(message)
{
    public bool InUse { get; } = inUse;
}