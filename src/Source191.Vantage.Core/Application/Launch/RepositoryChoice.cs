using Source191.Vantage.Domain;

namespace Source191.Vantage.Application.Launch;

public sealed record RepositoryChoice(string Name, FolderPath Path, bool IsRepository)
{
    /// <summary>Whether the filter typed in the wizard keeps this choice: part of its name, in any case.</summary>
    public bool Matches(string filter) => Name.Contains(filter, StringComparison.OrdinalIgnoreCase);
}