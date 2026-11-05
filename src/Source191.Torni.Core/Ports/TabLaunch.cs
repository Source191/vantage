using Source191.Torni.Domain;

namespace Source191.Torni.Ports;

/// <summary>A tab to open: what it shows, where it starts, and the PowerShell script it runs.</summary>
public sealed record TabLaunch(string Title, TabColour Colour, FolderPath StartingDirectory, string Script);