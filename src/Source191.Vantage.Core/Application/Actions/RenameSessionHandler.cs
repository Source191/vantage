using Source191.Vantage.Application.Effects;
using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;
using Source191.Vantage.Ports;

namespace Source191.Vantage.Application.Actions;

/// <summary>
/// The CLI cannot rename a background session, and resuming one with flags starts a copy, so the job is removed
/// (its transcript stays on disc) and resumed by session id under the new name: same session, new label. If the
/// resume fails the job is resumed under the old name so the session is not left unsupervised. A Vantage tab showing
/// the session is closed once the job is removed, and reopened under the new name in its old colour after the resume.
/// </summary>
public sealed class RenameSessionHandler(IClaudeCli claude, ITranscripts transcripts, ITabRegistry registrations, SessionTabs tabs, OpenTabHandler openTab) : IEffectHandler<RenameSession>
{
    public async Task<Outcome> HandleAsync(RenameSession effect, CancellationToken cancellationToken)
    {
        BackgroundSession session = effect.Session;
        SessionName newName = effect.NewName;
        if (!transcripts.HasMessages(session.SessionId.Value))
        {
            return new RenameSessionOutcome.NoMessages(session.Name);
        }

        TabColour? colour = (await registrations.ReadTabsAsync()).FirstOrDefault(tab => tab.Owner == new TabOwner.ForSession(session.SessionId))?.Colour;
        await claude.RemoveAsync(session.JobId.Value, cancellationToken);
        bool tabClosed = await tabs.CloseAsync(session.SessionId) == TabClosure.Closed;
        ResumeResult renamed = await claude.ResumeBackgroundAsync(session.SessionId.Value, session.WorkingDirectory.Value, newName, cancellationToken);
        if (renamed.Continued)
        {
            if (tabClosed)
            {
                await openTab.OpenAsync(session with { Name = newName.Value }, colour, cancellationToken);
            }

            return new RenameSessionOutcome.Renamed(session.Name, newName.Value, TabMoved: tabClosed);
        }

        // The port resumes under a name or under none: a listed name SessionName refuses resumes unnamed, so the bool is not needed.
        _ = SessionName.TryParse(session.Name, out SessionName? name);
        ResumeResult restored = await claude.ResumeBackgroundAsync(session.SessionId.Value, session.WorkingDirectory.Value, name, cancellationToken);

        return restored.Continued
            ? new RenameSessionOutcome.Restored(session.Name, renamed.Message, tabClosed)
            : new RenameSessionOutcome.Unsupervised(session.Name, renamed.Message, session.SessionId, restored.Message);
    }
}

/// <summary>What renaming did: renamed, refused, or failed and either restored or left for the user to resume.</summary>
public abstract record RenameSessionOutcome : Outcome
{
    public sealed record NoMessages(string Name) : RenameSessionOutcome;

    /// <summary>Renamed; TabMoved when a Vantage tab showing it was closed and one opened under the new name.</summary>
    public sealed record Renamed(string OldName, string NewName, bool TabMoved) : RenameSessionOutcome;

    /// <summary>Back under its old name; TabClosed when a Vantage tab showing it was closed and none reopened.</summary>
    public sealed record Restored(string OldName, string Failure, bool TabClosed) : RenameSessionOutcome;

    public sealed record Unsupervised(string OldName, string Failure, SessionId SessionId, string RestoreFailure) : RenameSessionOutcome;
}