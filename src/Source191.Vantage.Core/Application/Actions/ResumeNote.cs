using Source191.Vantage.Domain;

namespace Source191.Vantage.Application.Actions;

/// <summary>A session that did not resume, and why.</summary>
public abstract record ResumeNote(string Name)
{
    /// <summary>The session has no transcript, so there is nothing to resume.</summary>
    public sealed record NeverSentAMessage(string Name) : ResumeNote(Name);

    /// <summary>Claude refused to remove the job, so the session keeps it, was not resumed and gets no tab.</summary>
    public sealed record NotRemoved(string Name, string Failure) : ResumeNote(Name);

    /// <summary>The CLI refused the resume with this message; the job was removed first, so the session id is what brings it back.</summary>
    public sealed record Failed(string Name, string Failure, SessionId SessionId) : ResumeNote(Name);
}