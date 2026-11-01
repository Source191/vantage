using Source191.Vantage.Domain;

namespace Source191.Vantage.Application.Detail;

/// <summary>Whether Vantage created the worktree, with its record; one whose records cannot be read reads as not by Vantage.</summary>
public abstract record WorktreeOrigin
{
    public sealed record CreatedByVantage(CreatedWorktree Record) : WorktreeOrigin;

    public sealed record NotByVantage : WorktreeOrigin;
}