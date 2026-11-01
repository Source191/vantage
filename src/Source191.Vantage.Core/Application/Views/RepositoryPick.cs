using Source191.Vantage.Application.Launch;

namespace Source191.Vantage.Application.Views;

/// <summary>The repository the wizard would start in: the fixed one, the selected choice, or none while the list is empty.</summary>
public abstract record RepositoryPick
{
    public sealed record None : RepositoryPick;

    public sealed record Picked(RepositoryChoice Choice) : RepositoryPick;
}