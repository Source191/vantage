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

    /// <summary>The branch name holds a character a batch file cannot be given, so az.cmd was never started for it.</summary>
    BranchNotGivenToAz,
}

/// <summary>A row's pull request, or the reason it has none.</summary>
public abstract record PullRequestLookup
{
    public sealed record Found(PullRequest PullRequest) : PullRequestLookup;

    /// <summary>Host is known once the origin remote has been read, so the footer can name that host's CLI.</summary>
    public sealed record Missing(PullRequestGap Gap, PullRequestHost? Host = null) : PullRequestLookup;
}