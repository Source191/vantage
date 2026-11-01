namespace Source191.Vantage.Application.Diagnostics;

/// <summary>A part of a refresh that is timed on its own.</summary>
public enum RefreshPart
{
    /// <summary>The Sessions tab's <c>claude agents</c> listing.</summary>
    SessionsListing,

    /// <summary>The Sessions rows' enrichment (git, transcripts, processes) after the listing.</summary>
    SessionsEnrichment,

    /// <summary>The Worktrees tab's inventory scan, without its wait for the first listing.</summary>
    WorktreesScan,

    /// <summary>A pull-request refresh that started at least one lookup, until the last one lands.</summary>
    PullRequestRefresh,
}