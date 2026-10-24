using Source191.Vantage.Application.Effects;
using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;
using Source191.Vantage.Ports;

namespace Source191.Vantage.Application.Actions;

/// <summary>
/// The CLI cannot rename a background session, and resuming one with flags starts a copy, so the job is removed
/// (its transcript stays on disc) and resumed by session id under the new name: same session, new label. If the
/// resume fails the job is resumed under the old name so the session is not left unsupervised.
/// </summary>
public sealed class RenameSessionHandler(IClaudeCli claude, ITranscripts transcripts) : IEffectHandler<RenameSession>
{
    public async Task<string> HandleAsync(RenameSession effect, CancellationToken cancellationToken)
    {
        Session session = effect.Row.Session;
        SessionName newName = effect.NewName;
        if (!transcripts.HasMessages(session.SessionId))
        {
            return $"{session.Name} has no messages yet, so it cannot be renamed; send it something first.";
        }

        await claude.RemoveAsync(session.JobId!, cancellationToken);
        ResumeResult renamed = await claude.ResumeBackgroundAsync(session.SessionId, session.WorkingDirectory, newName, cancellationToken);
        if (renamed.Continued)
        {
            return $"Renamed {session.Name} to {newName.Text}; it restarts idle and keeps its conversation.";
        }

        ResumeResult restored = await claude.ResumeBackgroundAsync(session.SessionId, session.WorkingDirectory, SessionName.TryParse(session.Name), cancellationToken);

        return restored.Continued
            ? $"Could not rename {session.Name}: {renamed.Message}; it is back under its old name."
            : $"Could not rename {session.Name}: {renamed.Message}; resume it with claude --bg --resume {session.SessionId} ({restored.Message}).";
    }
}

/// <summary>What renaming did: renamed, refused, or failed and either restored or left for the user to resume.</summary>
public abstract record RenameSessionOutcome : Outcome
{
    public sealed record NoMessages(string Name) : RenameSessionOutcome;

    public sealed record Renamed(string OldName, string NewName) : RenameSessionOutcome;

    public sealed record Restored(string OldName, string Failure) : RenameSessionOutcome;

    public sealed record Unsupervised(string OldName, string Failure, string SessionId, string RestoreFailure) : RenameSessionOutcome;
}