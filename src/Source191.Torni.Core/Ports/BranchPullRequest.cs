using Source191.Torni.Domain;

namespace Source191.Torni.Ports;

/// <summary>One branch's pull request in a host's listing.</summary>
public sealed record BranchPullRequest(BranchName Branch, PullRequest PullRequest);