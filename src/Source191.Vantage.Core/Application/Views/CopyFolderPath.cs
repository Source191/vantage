using Source191.Vantage.Domain;

namespace Source191.Vantage.Application.Views;

/// <summary>Put the folder's full path on the clipboard.</summary>
public sealed record CopyFolderPath(FolderPath Folder) : ViewEffect;