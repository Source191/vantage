namespace Source191.Vantage.Domain;

/// <summary>One pull request from a host's listing.</summary>
public sealed record PullRequest(PullRequestHost Host, int Number, Uri Url, PullRequestState State, DateTimeOffset CreatedAt);