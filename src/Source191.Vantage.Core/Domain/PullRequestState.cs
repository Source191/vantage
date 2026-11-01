namespace Source191.Vantage.Domain;

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