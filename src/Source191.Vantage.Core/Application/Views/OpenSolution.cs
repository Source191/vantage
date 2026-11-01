namespace Source191.Vantage.Application.Views;

/// <summary>Open a solution file with Windows' default application for it.</summary>
public sealed record OpenSolution(string File) : ViewEffect;