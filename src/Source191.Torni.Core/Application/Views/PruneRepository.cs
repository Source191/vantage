using Source191.Torni.Domain;

namespace Source191.Torni.Application.Views;

public sealed record PruneRepository(FolderPath Repository) : ViewEffect;