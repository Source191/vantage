using Source191.Torni.Domain;

namespace Source191.Torni.Application.Views;

/// <summary>Open the row's pull request in the default browser.</summary>
public sealed record OpenPullRequest(PullRequest PullRequest) : ViewEffect;