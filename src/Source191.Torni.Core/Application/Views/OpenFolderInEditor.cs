using Source191.Torni.Domain;

namespace Source191.Torni.Application.Views;

/// <summary>Open the folder in the editor chosen in settings.</summary>
public sealed record OpenFolderInEditor(FolderPath Folder) : ViewEffect;