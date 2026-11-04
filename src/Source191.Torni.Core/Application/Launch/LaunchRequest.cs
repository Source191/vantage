using Source191.Torni.Domain;

namespace Source191.Torni.Application.Launch;

/// <summary>What the user typed and chose in the wizard, before any rule is applied.</summary>
public sealed record LaunchRequest(
    RepositoryChoice Repository,
    string NameText,
    LaunchMode Mode,
    WorktreeBase Base,
    string OpeningPromptText);