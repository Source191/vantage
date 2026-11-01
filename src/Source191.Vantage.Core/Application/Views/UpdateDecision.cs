using Source191.Vantage.Domain;

namespace Source191.Vantage.Application.Views;

/// <summary>What the update key does with what the update check knows.</summary>
public abstract record UpdateDecision
{
    /// <summary>Show what changed from the running version up to the newest release, and offer the update; no sections when the notes could not be read.</summary>
    public sealed record Ask(ReleaseVersion Current, ReleaseVersion Newest, IReadOnlyList<ReleaseSection> Sections) : UpdateDecision;

    /// <summary>Nothing to update to: show the message instead.</summary>
    public sealed record Refuse(StatusMessage Message) : UpdateDecision;
}