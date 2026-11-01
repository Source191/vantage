namespace Source191.Vantage.Domain;

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