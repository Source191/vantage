namespace Source191.Torni.Application.Views;

/// <summary>Open a solution file with Windows' default application for it.</summary>
public sealed record OpenSolution(string File) : ViewEffect;