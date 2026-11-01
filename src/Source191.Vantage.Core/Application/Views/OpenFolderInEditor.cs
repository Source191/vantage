using Source191.Vantage.Domain;

namespace Source191.Vantage.Application.Views;

/// <summary>Open the folder in the editor chosen in settings.</summary>
public sealed record OpenFolderInEditor(FolderPath Folder) : ViewEffect;