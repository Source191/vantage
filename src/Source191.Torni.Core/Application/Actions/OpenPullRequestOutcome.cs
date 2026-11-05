using Source191.Torni.Application.Effects;
using Source191.Torni.Domain;

namespace Source191.Torni.Application.Actions;

/// <summary>A pull request's page was handed to the default browser.</summary>
public sealed record OpenPullRequestOutcome(PullRequestHost Host, int Number) : Outcome;