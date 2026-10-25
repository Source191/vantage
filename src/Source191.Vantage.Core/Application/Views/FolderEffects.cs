using Source191.Vantage.Domain;

namespace Source191.Vantage.Application.Views;

/// <summary>Put the folder's full path on the clipboard.</summary>
public sealed record CopyFolderPath(FolderPath Folder) : ViewEffect;

/// <summary>Open the folder in the editor chosen in settings.</summary>
public sealed record OpenFolderInEditor(FolderPath Folder) : ViewEffect;

/// <summary>Look for the folder's solution and open it; a dialog because several solutions need a question.</summary>
public sealed record FindSolution(FolderPath Folder, string RepositoryName) : ViewEffect, IDialogEffect;

/// <summary>Open a solution file with Windows' default application for it.</summary>
public sealed record OpenSolution(string File) : ViewEffect;