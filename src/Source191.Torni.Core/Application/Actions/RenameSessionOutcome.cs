using Source191.Torni.Application.Effects;
using Source191.Torni.Domain;

namespace Source191.Torni.Application.Actions;

/// <summary>What renaming did: renamed, refused, or failed and either restored or left for the user to resume.</summary>
public abstract record RenameSessionOutcome : Outcome
{
    public sealed record NoMessages(ListedName Name) : RenameSessionOutcome;

    /// <summary>Renamed; TabMoved when a Torni tab showing it was closed and one opened under the new name.</summary>
    public sealed record Renamed(ListedName OldName, SessionName NewName, bool TabMoved) : RenameSessionOutcome;

    /// <summary>Back under its old name; TabClosed when a Torni tab showing it was closed and none reopened.</summary>
    public sealed record Restored(ListedName OldName, string Failure, bool TabClosed) : RenameSessionOutcome;

    /// <summary>Claude refused to remove the job, so nothing was resumed or closed and the session is still listed under its old name.</summary>
    public sealed record NotRemoved(ListedName OldName, string Failure) : RenameSessionOutcome;

    public sealed record Unsupervised(ListedName OldName, string Failure, SessionId SessionId, string RestoreFailure) : RenameSessionOutcome;
}