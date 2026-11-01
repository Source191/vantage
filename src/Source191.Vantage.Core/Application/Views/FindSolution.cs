using Source191.Vantage.Domain;

namespace Source191.Vantage.Application.Views;

/// <summary>Look for the folder's solution and open it; a dialog because several solutions need a question.</summary>
public sealed record FindSolution(FolderPath Folder, string RepositoryName) : ViewEffect, IDialogEffect;