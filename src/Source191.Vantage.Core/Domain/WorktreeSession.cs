namespace Source191.Vantage.Domain;

/// <summary>The session running in a worktree, if any: by id, because names are not unique, and by name, which a refusal to remove it says.</summary>
public abstract record WorktreeSession
{
    public sealed record None : WorktreeSession;

    public sealed record Running(SessionId Id, string Name) : WorktreeSession;
}