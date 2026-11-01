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

        TabRegistration? earlier = (await registrations.ReadTabsAsync()).FirstOrDefault(tab => tab.Owner == new TabOwner.ForSession(session.SessionId));
        try
        {
            await claude.RemoveAsync(session.JobId.Value, cancellationToken);
        }
        catch (ClaudeCliException refusal)
        {
            return new RenameSessionOutcome.NotRemoved(session.Name, refusal.Message);
        }

        bool tabClosed = await tabs.CloseAsync(session.SessionId) == TabClosure.Closed;
        ResumeResult renamed = await claude.ResumeBackgroundAsync(session.SessionId.Value, session.WorkingDirectory.Value, newName, cancellationToken);
        if (renamed is ResumeResult.Continued)
        {
            if (tabClosed)
            {
                BackgroundSession renamedSession = session with { Name = newName.Value };
                await (earlier is null ? openTab.OpenAsync(renamedSession, cancellationToken) : openTab.OpenAsync(renamedSession, earlier.Colour, cancellationToken));
            }

            return new RenameSessionOutcome.Renamed(session.Name, newName.Value, TabMoved: tabClosed);
        }

        // A listed name SessionName refuses is restored unnamed.
        ResumeResult restored = SessionName.TryParse(session.Name, out SessionName? name)
            ? await claude.ResumeBackgroundAsync(session.SessionId.Value, session.WorkingDirectory.Value, name, cancellationToken)
            : await claude.ResumeBackgroundAsync(session.SessionId.Value, session.WorkingDirectory.Value, cancellationToken);

        return restored is ResumeResult.Continued
            ? new RenameSessionOutcome.Restored(session.Name, renamed.Message, tabClosed)
            : new RenameSessionOutcome.Unsupervised(session.Name, renamed.Message, session.SessionId, restored.Message);
    }
}