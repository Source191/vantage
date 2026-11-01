using Source191.Vantage.Domain;
using System.Diagnostics;

namespace Source191.Vantage.Application.Views;

/// <summary>
/// A listed session with what git, the state store and the transcript folder add to it. Location says whether it runs
/// in a repository (and which main checkout) or outside one; Repository is the name either gives. Head is Unknown outside a
/// repository or when git did not name a branch. Tab says whether a live Vantage tab shows the session, and in which
/// colour. HasTranscript is false for a session never sent a message: Claude writes nothing to resume from until then,
/// so every caller must say which it is.
/// </summary>
public sealed record SessionRow(Session Session, SessionLocation Location, Head Head, LiveTab Tab, bool HasTranscript)
{
    /// <summary>The repository's name, or the folder's outside a repository: what REPOSITORY, the filter and the folder keys read.</summary>
    public string Repository => Location switch
    {
        SessionLocation.Outside outside => outside.Folder,
        SessionLocation.InRepository inRepository => inRepository.Name,
        _ => throw new UnreachableException(),
    };

    /// <summary>The branch's pull request, or why there is none; a row outside a repository has no branch to look up.</summary>
    public PullRequestLookup PullRequest { get; init; } = new PullRequestLookup.Missing(PullRequestGap.NoBranch);

    /// <summary>What the session's process and everything it started use; nothing to measure for a session with no process, or whose process has gone.</summary>
    public SessionMemory Memory { get; init; } = new SessionMemory.NothingToMeasure();

}