using Source191.Torni.Application.Effects;

namespace Source191.Torni.Application.Actions;

/// <summary>The folder's full path is on the clipboard.</summary>
public sealed record FolderPathCopied(string Path) : Outcome;