using Source191.Vantage.Application.Effects;
using Source191.Vantage.Domain;

namespace Source191.Vantage.Application.Actions;

/// <summary>What removing a repository root did: removed it and saved, or kept the only one.</summary>
public abstract record RemoveRepositoryRootOutcome : Outcome
{
    public sealed record Removed(FolderPath Root) : RemoveRepositoryRootOutcome;

    public sealed record OnlyRoot : RemoveRepositoryRootOutcome;
}