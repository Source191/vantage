using Source191.Torni.Domain;

namespace Source191.Torni.Application.Detail;

/// <summary>Whether Torni created the worktree, with its record; one whose records cannot be read reads as not by Torni.</summary>
public abstract record WorktreeOrigin
{
    public sealed record CreatedByTorni(CreatedWorktree Record) : WorktreeOrigin;

    public sealed record NotByTorni : WorktreeOrigin;
}