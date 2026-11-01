using Source191.Vantage.Domain;

namespace Source191.Vantage.Application.Launch;

/// <summary>What the user typed and chose in the wizard, before any rule is applied.</summary>
public sealed record LaunchRequest(
    RepositoryChoice Repository,
    string NameText,
    LaunchMode Mode,
    WorktreeBase Base,
    string OpeningPromptText);