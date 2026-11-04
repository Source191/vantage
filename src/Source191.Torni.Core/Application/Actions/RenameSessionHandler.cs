using Source191.Torni.Application.Effects;
using Source191.Torni.Application.Views;
using Source191.Torni.Domain;
using Source191.Torni.Ports;

namespace Source191.Torni.Application.Actions;

/// <summary>
/// The CLI cannot rename a background session, and resuming one with flags starts a copy, so the job is removed
/// (its transcript stays on disc) and resumed by session id under the new name: same session, new label. If the
/// resume fails the job is resumed under the old name so the session is not left unsupervised. A Torni tab showing
/// the session is closed once the job is removed, and reopened under the new name in its old colour after the resume.
/// </summary>
public sealed class RenameSessionHandler(IClaudeCli claude, ITranscripts transcripts, ITabRegistry registrations, SessionTabs tabs, OpenTabHandler openTab) : IEffectHandler<RenameSession>
{
    public async Task<Outcome> HandleAsync(RenameSession effect, CancellationToken cancellationToken)
    {
        Session.Background session = effect.Session;
        SessionName newName = effect.NewName;
        if (!transcripts.HasMessages(session.SessionId))
        {
            return new RenameSessionOutcome.NoMessages(session.Name);
        }

        TabRegistration? earlier = new TabRegistrations(await registrations.ReadTabsAsync()).For(session.SessionId) is [TabRegistration first, ..] ? first : null;
        try
        {
            await claude.RemoveAsync(session.JobId, cancellationToken);
        }
        catch (ClaudeCliException refusal)
        {
            return new RenameSessionOutcome.NotRemoved(session.Name, refusal.Message);
        }

        bool tabClosed = await tabs.CloseAsync(session.SessionId) == TabClosure.Closed;
        ResumeResult renamed = await claude.ResumeBackgroundAsync(session.SessionId, session.WorkingDirectory, new ListedName.Named(newName), cancellationToken);
        if (renamed is ResumeResult.Continued)
        {
            if (tabClosed)
            {
                Session.Background renamedSession = session with { Name = new ListedName.Named(newName) };
                await (earlier is null ? openTab.OpenAsync(renamedSession, cancellationToken) : openTab.OpenAsync(renamedSession, earlier.Colour, cancellationToken));
            }

            return new RenameSessionOutcome.Renamed(session.Name, newName, TabMoved: tabClosed);
        }

        ResumeResult restored = await claude.ResumeBackgroundAsync(session.SessionId, session.WorkingDirectory, session.Name, cancellationToken);

        return restored is ResumeResult.Continued
            ? new RenameSessionOutcome.Restored(session.Name, renamed.Message, tabClosed)
            : new RenameSessionOutcome.Unsupervised(session.Name, renamed.Message, session.SessionId, restored.Message);
    }
}