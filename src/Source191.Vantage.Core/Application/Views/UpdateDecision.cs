using Source191.Vantage.Domain;

namespace Source191.Vantage.Application.Views;

/// <summary>What the update key does with what the update check knows.</summary>
public abstract record UpdateDecision
{
    /// <summary>Ask whether to update to this release.</summary>
    public sealed record Ask(ReleaseVersion Newest) : UpdateDecision;

    /// <summary>Nothing to update to: show the message instead.</summary>
    public sealed record Refuse(StatusMessage Message) : UpdateDecision;
}