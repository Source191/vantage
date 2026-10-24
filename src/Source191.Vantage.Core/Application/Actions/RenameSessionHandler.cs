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
    public async Task<Outcome> HandleAsync(RenameSession effect, CancellationToken cancellationToken)
    {
        Session session = effect.Row.Session;
        SessionName newName = effect.NewName;
        if (!transcripts.HasMessages(session.SessionId))
        {
            return new RenameSessionOutcome.NoMessages(session.Name);
        }

        await claude.RemoveAsync(session.JobId!, cancellationToken);
        ResumeResult renamed = await claude.ResumeBackgroundAsync(session.SessionId, session.WorkingDirectory.Value, newName, cancellationToken);
        if (renamed.Continued)
        {
            return new RenameSessionOutcome.Renamed(session.Name, newName.Text);
        }

        ResumeResult restored = await claude.ResumeBackgroundAsync(session.SessionId, session.WorkingDirectory.Value, SessionName.TryParse(session.Name), cancellationToken);

        return restored.Continued
            ? new RenameSessionOutcome.Restored(session.Name, renamed.Message)
            : new RenameSessionOutcome.Unsupervised(session.Name, renamed.Message, session.SessionId, restored.Message);
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