using Source191.Vantage.Domain;

namespace Source191.Vantage.Application.Views;

/// <summary>Open the row's pull request in the default browser.</summary>
public sealed record OpenPullRequest(PullRequest PullRequest) : ViewEffect;