namespace Source191.Vantage.Domain;

/// <summary>The hosts Vantage asks for pull requests.</summary>
public enum PullRequestHost
{
    GitHub,
    AzureDevOps,
}

/// <summary>A pull request's state in its host's own words: GitHub's open, draft, merged and closed; Azure DevOps's active, draft, completed and abandoned.</summary>
public enum PullRequestState
{
    Open,
    Draft,
    Merged,
    Closed,
    Active,
    Completed,
    Abandoned,
}

/// <summary>One pull request from a host's listing.</summary>
public sealed record PullRequest(PullRequestHost Host, int Number, Uri Url, PullRequestState State, DateTimeOffset CreatedAt);

public static class PullRequestStates
{
    /// <summary>Whether the pull request can still merge: open, draft or active.</summary>
    public static bool IsOpen(this PullRequestState state) => state switch
    {
        PullRequestState.Open => true,
        PullRequestState.Draft => true,
        PullRequestState.Active => true,
        PullRequestState.Merged => false,
        PullRequestState.Closed => false,
        PullRequestState.Completed => false,
        PullRequestState.Abandoned => false,
    };
}

/// <summary>Which of a branch's pull requests the tables show: the open one, otherwise the most recent.</summary>
public static class PullRequests
{
    public static PullRequest? ForBranch(IReadOnlyList<PullRequest> forBranch) =>
        forBranch.FirstOrDefault(pullRequest => pullRequest.State.IsOpen()) ?? forBranch.MaxBy(pullRequest => pullRequest.CreatedAt);
}
