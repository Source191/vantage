using Source191.Vantage.Domain;

namespace Source191.Vantage.Application.Views;

public sealed record PruneRepository(FolderPath Repository) : ViewEffect;