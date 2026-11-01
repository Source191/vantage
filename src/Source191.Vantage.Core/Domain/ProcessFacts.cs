namespace Source191.Vantage.Domain;

/// <summary>A process as the operating system's table lists it: its id, its parent's id and its executable name without the extension.</summary>
public sealed record ProcessFacts(int ProcessId, int ParentId, string Name);