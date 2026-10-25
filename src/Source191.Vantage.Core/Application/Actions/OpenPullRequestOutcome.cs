using Source191.Vantage.Application.Effects;
using Source191.Vantage.Domain;

namespace Source191.Vantage.Application.Actions;

/// <summary>A pull request's page was handed to the default browser.</summary>
public sealed record OpenPullRequestOutcome(PullRequestHost Host, int Number) : Outcome;