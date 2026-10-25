namespace Source191.Vantage.Domain;

/// <summary>Why a row shows no pull request.</summary>
public enum PullRequestGap
{
    NoBranch,
    NoRemote,
    UnknownHost,
    CliMissing,
    NotSignedIn,
    Looking,
    Failed,
    NoPullRequest,
}

/// <summary>A row's pull request, or the reason it has none.</summary>
public abstract record PullRequestLookup
{
    public sealed record Found(PullRequest PullRequest) : PullRequestLookup;

    public sealed record Missing(PullRequestGap Gap) : PullRequestLookup;
}
