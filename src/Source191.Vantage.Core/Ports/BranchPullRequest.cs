using Source191.Vantage.Domain;

namespace Source191.Vantage.Ports;

/// <summary>One branch's pull request in a host's listing.</summary>
public sealed record BranchPullRequest(BranchName Branch, PullRequest PullRequest);