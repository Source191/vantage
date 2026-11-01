namespace Source191.Vantage.Domain;

/// <summary>Which of a branch's pull requests the tables show: the open one, otherwise the most recent, and NoPullRequest when it has none.</summary>
public static class PullRequests
{
    public static PullRequestLookup ForBranch(IReadOnlyList<PullRequest> forBranch) =>
        (forBranch.FirstOrDefault(pullRequest => pullRequest.State.IsOpen()) ?? forBranch.MaxBy(pullRequest => pullRequest.CreatedAt)) is PullRequest shown
            ? new PullRequestLookup.Found(shown)
            : new PullRequestLookup.Missing(PullRequestGap.NoPullRequest);
}