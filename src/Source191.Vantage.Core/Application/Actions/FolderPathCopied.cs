using Source191.Vantage.Application.Effects;

namespace Source191.Vantage.Application.Actions;

/// <summary>The folder's full path is on the clipboard.</summary>
public sealed record FolderPathCopied(string Path) : Outcome;