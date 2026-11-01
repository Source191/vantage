namespace Source191.Vantage.Domain;

/// <summary>A process's parent: its identifier and executable name without extension (for example WindowsTerminal, pwsh).</summary>
public sealed record ProcessParent(int ProcessId, string Name);