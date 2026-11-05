using Source191.Torni.Domain;

namespace Source191.Torni.Application.Views;

/// <summary>Put the folder's full path on the clipboard.</summary>
public sealed record CopyFolderPath(FolderPath Folder) : ViewEffect;