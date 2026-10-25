using Source191.Vantage.Application.Effects;

namespace Source191.Vantage.Application.Actions;

/// <summary>A solution was handed to Windows to open with its default application.</summary>
public sealed record OpenedSolution(string FileName) : Outcome;