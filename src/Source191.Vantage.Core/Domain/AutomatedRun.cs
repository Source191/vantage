namespace Source191.Vantage.Domain;

/// <summary>
/// A claude started by a program rather than a person (the Agent SDK, <c>claude -p</c>), such as a plugin's background
/// review: listed beside the sessions but not one, so it is never a row and takes no keys.
/// </summary>
public sealed record AutomatedRun(SessionId SessionId, FolderPath WorkingDirectory, int ProcessId);