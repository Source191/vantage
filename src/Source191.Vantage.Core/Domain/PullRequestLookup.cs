namespace Source191.Vantage.Domain;

/// <summary>A row's pull request, or the reason it has none.</summary>
public abstract record PullRequestLookup
{
    public sealed record Found(PullRequest PullRequest) : PullRequestLookup;

    /// <summary>Host is known once the origin remote has been read, so the status line can name that host's CLI.</summary>
    public sealed record Missing(PullRequestGap Gap, LookupHost Host) : PullRequestLookup
    {
        /// <summary>A gap found before the origin remote was read.</summary>
        public Missing(PullRequestGap gap)
            : this(gap, new LookupHost.NotRead())
        {
        }
    }
}